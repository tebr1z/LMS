using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using System.Text.Json;

namespace LMS.Application.Features.Quizzes.Commands.CreateQuiz;

public class CreateQuizCommandHandler : IRequestHandler<CreateQuizCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public CreateQuizCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(CreateQuizCommand request, CancellationToken cancellationToken)
    {
        // Verify assignment exists and is of type Test
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        if (assignment.AssignmentType != AssignmentType.Test)
        {
            throw new InvalidOperationException("Quiz can only be created for assignments of type Test.");
        }

        // Verify user is authorized (Teacher or Admin)
        var user = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        if (user.Role != UserRole.Teacher && user.Role != UserRole.MasterAdmin && user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher or Admin can create quizzes.");
        }

        // Check if quiz already exists for this assignment
        var existingQuiz = await _unitOfWork.Quizzes.GetByAssignmentIdAsync(request.AssignmentId);
        if (existingQuiz != null)
        {
            throw new InvalidOperationException($"Quiz already exists for Assignment with ID {request.AssignmentId}.");
        }

        // Create Quiz
        var quiz = new Quiz
        {
            AssignmentId = request.AssignmentId,
            QuizId = Guid.NewGuid().ToString(), // Generate unique QuizId
            TimeLimitSeconds = request.TimeLimitSeconds,
            ShuffleQuestions = request.ShuffleQuestions,
            PassingThreshold = request.PassingThreshold,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Quizzes.AddAsync(quiz);
        await _unitOfWork.SaveChangesAsync(); // Save to get Quiz.Id

        // Create QuizQuestions - need to add repository for QuizQuestion
        var order = 0;
        foreach (var questionDto in request.Questions.OrderBy(q => q.Order))
        {
            var question = new QuizQuestion
            {
                QuizId = quiz.Id,
                Text = questionDto.Text,
                Options = JsonSerializer.Serialize(questionDto.Options), // Store as JSON
                CorrectAnswer = questionDto.CorrectAnswer,
                Points = questionDto.Points,
                Order = questionDto.Order > 0 ? questionDto.Order : order,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.QuizQuestions.AddAsync(question);
            order++;
        }

        await _unitOfWork.SaveChangesAsync();

        return quiz.Id;
    }
}

