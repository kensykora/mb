using AutoMapper;
using MB.Telegram.Models;
using Telegram.Bot.Types;

namespace MB.Telegram
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Update, MBUser>()
                .ForMember(dest => dest.Service, opt => opt.MapFrom(src => ChatServices.Telegram))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.Message.From.Id))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{Prefix.Telegram}-{src.Message.From.Id.ToString()}"))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => (src.Message.From.FirstName + " " + src.Message.From.LastName).Trim()))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Message.From.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Message.From.LastName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Message.From.Username))
                .ForMember(dest => dest.SpotifyId, opt => opt.Ignore());

            CreateMap<TelegramUser, MBUser>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{Prefix.Telegram}-{src.Id.ToString()}"))
                .ForMember(dest => dest.ChatServiceAuthDate, opt => opt.MapFrom(src => src.AuthDate))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.PhotoUrl));
        }
    }
}