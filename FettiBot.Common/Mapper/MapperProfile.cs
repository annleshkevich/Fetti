using AutoMapper;
using FettiBot.Common.DTOs;
using FettiBot.Model.DatabaseModels;

namespace FettiBot.Common.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Client, ClientDto>().ReverseMap();
        }
    }
}
