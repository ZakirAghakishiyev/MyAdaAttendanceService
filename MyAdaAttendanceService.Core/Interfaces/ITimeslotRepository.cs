using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ITimeslotRepository : IRepository<Timeslot>
{
    Task<List<Timeslot>> GetByDayAsync(string day);
}