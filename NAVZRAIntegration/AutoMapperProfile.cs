using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using NAVReadReference;
using NAVUpdateReference;
using NAVZRAIntegration.Models;

namespace NAVZRAIntegration
{
    class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ESDInvoiceItem, SalesInvLine>()
                .ForMember(dest => dest.Unit_Price,
                opts => opts.MapFrom(
                    src => src.UnitPrice))
                .ForMember(dest => dest.Tax_Label,
                opts => opts.MapFrom(
                    src => src.TaxLabels))
                .ReverseMap();            
            CreateMap<ESDInvoiceItem, P_Sales_Inv_Line>()
                .ForMember(dest => dest.Unit_Price,
                opts => opts.MapFrom(
                    src => src.UnitPrice))
                .ForMember(dest => dest.Amount,
                opts => opts.MapFrom(
                    src => src.TotalAmount))
                .ReverseMap();
            CreateMap<ESDInvoiceHeader, ZRASalesInvoice2>()
                .ForMember(dest => dest.SalesInvLines,
                opts => opts.MapFrom(
                    src => src.Items))
                    .ReverseMap();
            CreateMap<ESDInvoiceHeader, SalesInvHeader>()                 
                    .ReverseMap();            
            CreateMap<ZRASalesInvoice2, SalesInvHeader>()
                .ForMember(dest => dest.SalesInvLine,
                opts => opts.MapFrom(
                    src => src.SalesInvLines))
                .ForMember(dest => dest.No_,
                opts => opts.MapFrom(
                    src => src.No))
                    .ReverseMap();
            CreateMap<P_Sales_Inv_Line, SalesInvLine>()                 
                    .ReverseMap();
            CreateMap<P_Sales_Inv_Line, ResponseLine>()                 
                    .ReverseMap();
            CreateMap<string, DateTime>().ConvertUsing<StringToDateTimeConverter>();
        }

        public class StringToDateTimeConverter : ITypeConverter<string, DateTime>
        {
            public DateTime Convert(string source, DateTime destination, ResolutionContext context)
            {
                object objDateTime = source;
                DateTime dateTime;

                if (objDateTime == null)
                {
                    return default(DateTime);
                }

                if (DateTime.TryParse(objDateTime.ToString(), out dateTime))
                {
                    return dateTime;
                }

                return default(DateTime);
            }
        }
    }
}
