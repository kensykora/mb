using System;
using AutoMapper;
using MB.Telegram.Models;
using Telegram.Bot.Types;

namespace MB.Telegram
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Message, MBUser>()
                .ForMember(dest => dest.Service, opt => opt.MapFrom(src => ChatServices.Telegram))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.From.Id))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{Prefix.Telegram}|{src.From.Id.ToString()}"))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => (src.From.FirstName + " " + src.From.LastName).Trim()))
                .ForMember(dest => dest.ServiceAuthDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.From.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.From.LastName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.From.Username))
                .ForMember(dest => dest.SpotifyId, opt => opt.Ignore());

            CreateMap<TelegramUser, MBUser>()
                .ForMember(dest => dest.Service, opt => opt.MapFrom(src => ChatServices.Telegram))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => (src.FirstName + " " + src.LastName).Trim()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{Prefix.Telegram}|{src.Id.ToString()}"))
                .ForMember(dest => dest.ServiceAuthDate, opt => opt.MapFrom(src => src.AuthDate))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.PhotoUrl));

            CreateMap<User, MBUser>()
                .ForMember(dest => dest.Service, opt => opt.MapFrom(src => ChatServices.Telegram))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => (src.FirstName + " " + src.LastName).Trim()))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{Prefix.Telegram}|{src.Id.ToString()}"));
        }
    }
}