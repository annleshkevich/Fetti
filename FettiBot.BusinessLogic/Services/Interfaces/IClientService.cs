using FettiBot.Common.DTOs;

namespace FettiBot.BusinessLogic.Services.Interfaces
{
    public interface IClientService
    {
        IEnumerable<ClientDto> Get();
    }
}
