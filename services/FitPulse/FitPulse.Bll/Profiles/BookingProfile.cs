using AutoMapper;
using FitPulse.Bll.Dtos;
using FitPulse.Domain.Entities;

namespace FitPulse.Bll.Profiles;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Client, ClientDto>();
        CreateMap<CreateClientDto, Client>()
            .ForMember(dest => dest.ExternalUserId, opt => opt.MapFrom(_ => Guid.NewGuid()));

        CreateMap<PaymentRecord, PaymentDto>();

        CreateMap<Booking, BookingDto>();
    }
}