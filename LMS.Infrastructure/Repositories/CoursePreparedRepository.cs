using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;

namespace LMS.Infrastructure.Repositories;

public class CoursePreparedRepository : EfRepository<CoursePrepared>, ICoursePreparedRepository
{
    public CoursePreparedRepository(LmsDbContext context) : base(context)
    {
    }
}

