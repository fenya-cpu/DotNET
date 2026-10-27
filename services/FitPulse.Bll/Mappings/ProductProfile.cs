namespace FitPulse.Bll.Mappings;

using AutoMapper;
using FitPulse.Bll.DTOs;
using FitPulse.Domain.Entities;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Description, opt =>
                opt.MapFrom(src => src.Detail != null ? src.Detail.Description : null))
            .ForMember(dest => dest.Categories, opt =>
                opt.MapFrom(src => src.ProductCategories != null
                    ? src.ProductCategories
                        .Where(pc => pc != null && pc.Category != null)
                        .Select(pc => pc.Category.Name)
                        .ToList()
                    : new List<string>()));
    }
}
