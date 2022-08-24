using AutoMapper;
using FettiBot.BusinessLogic.Services.Interfaces;
using FettiBot.Common.DTOs;
using FettiBot.Model;
using Microsoft.EntityFrameworkCore;

namespace FettiBot.BusinessLogic.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        public ClientService(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public IEnumerable<ClientDto> Get()
        {
            var user = _context.Clients.AsNoTracking().ToList();
            var userDtos = _mapper.Map<List<ClientDto>>(user);
            return userDtos;
        }
    }
}
