using System.Threading.Tasks;
using PRN232.Plagiarism.Application.Events;

namespace PRN232.Plagiarism.Application.Interfaces;

public interface IPlagiarismAlertPublisher
{
    Task PublishAlertAsync(PlagiarismAlertEvent alertEvent);
}
