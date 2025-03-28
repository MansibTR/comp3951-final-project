﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GasStationPOS.Core.Models.ProductModels;
using GasStationPOS.Core.Services;
using GasStationPOS.UI.ViewDataTransferObjects.ProductDTOs;

namespace GasStationPOS.UI.ViewDataTransferObjects
{
    /// <summary>
    /// MappingProfiles class creates mappings between a Model and its corresponding DTO, for easier copying of data from the DTO instance to a model instance, and vice versa.
    /// It also ensures the UI data matches the required data in the data model.
    /// Can be used for UI - Model, and Database - Model
    /// Source:
    /// https://docs.automapper.org/en/stable/Configuration.html#profile-instances
    /// </summary>
    class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // Map the relevant fields between Product and ProductDTO
            CreateProductMappingProfile();

            // Map the relevant fields between FuelProduct and FuelProductDTO
            CreateFuelProductMappingProfile();

            // Map the relevant fields between RetailProduct and RetailProductDTO
            CreateRetailProductMappingProfile();

        }

        /// <summary>
        /// Maps the relevant fields between Product and ProductDTO
        /// </summary>
        private void CreateProductMappingProfile()
        {
            // Product -> ProductDTO
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductNameDescription, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.PriceDollars, opt => opt.MapFrom(src => src.PriceDollars))
                .Include<RetailProduct, RetailProductDTO>() // mapping inheritance for retail products
                .Include<FuelProduct, FuelProductDTO>();    // mapping inheritance for fuel products

            // ProductDTO -> Product
            CreateMap<ProductDTO, Product>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductNameDescription))
                .ForMember(dest => dest.PriceDollars, opt => opt.MapFrom(src => src.PriceDollars));
        }

        /// <summary>
        /// Maps the relevant fields between FuelProduct and FuelProductDTO
        /// </summary>
        private void CreateFuelProductMappingProfile()
        {
            // PriceDollars for FuelProduct (inherited from Product) is the PRICE ($)/LITRE.
            // To display the price of the FuelProduct in cents (¢):
            // will need to manually divide the FuelProductDTO value by 100 to display in ¢

            // FuelProduct -> FuelProductDTO
            CreateMap<FuelProduct, FuelProductDTO>()
                 .ForMember(dest => dest.FuelGrade, opt => opt.MapFrom(src => src.FuelGrade));
                 //.ForMember(dest => dest.FuelVolumeLitres, opt => opt.MapFrom(src => src.FuelVolumeLitres))
                 //.ForMember(dest => dest.PriceDollars, opt => opt.MapFrom(src => src.PriceDollars)); // <need to manually divide the dto value by 100 to display in ¢>

            // FuelProductDTO -> FuelProduct
            CreateMap<FuelProductDTO, FuelProduct>()
                 .ForMember(dest => dest.FuelGrade, opt => opt.MapFrom(src => src.FuelGrade));
                 //.ForMember(dest => dest.FuelVolumeLitres, opt => opt.MapFrom(src => src.FuelVolumeLitres))
                 //.ForMember(dest => dest.PriceDollars, opt => opt.MapFrom(src => src.PriceDollars));
        }

        /// <summary>
        /// Maps the relevant fields between RetailProduct and RetailProductDTO
        /// </summary>
        private void CreateRetailProductMappingProfile()
        {
            // RetailProduct -> RetailProductDTO
            CreateMap<RetailProduct, RetailProductDTO>()
                 //.ForMember(dest => dest.RetailCategory, opt => opt.MapFrom(src => src.RetailCategory))
                 .ForMember(dest => dest.ProductVolumeLitres, opt => opt.MapFrom(src => src.ProductVolumeLitres))
                 .ForMember(dest => dest.ProductSizeVariation, opt => opt.MapFrom(src => src.ProductSizeVariation)); // <need to manually divide the dto value by 100 to display in ¢>

            // RetailProductDTO -> RetailProduct
            CreateMap<RetailProductDTO, RetailProduct>()
                 //.ForMember(dest => dest.RetailCategory, opt => opt.MapFrom(src => src.RetailCategory))
                 .ForMember(dest => dest.ProductVolumeLitres, opt => opt.MapFrom(src => src.ProductVolumeLitres))
                 .ForMember(dest => dest.ProductSizeVariation, opt => opt.MapFrom(src => src.ProductSizeVariation));


            // For creating clones of retail products when adding new items to user cart ===

            // RetailProductDTO -> RetailProductDTO
            CreateMap<RetailProductDTO, RetailProductDTO>()
                 //.ForMember(dest => dest.RetailCategory, opt => opt.MapFrom(src => src.RetailCategory))
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.ProductNameDescription, opt => opt.MapFrom(src => src.ProductNameDescription))
                 .ForMember(dest => dest.PriceDollars, opt => opt.MapFrom(src => src.PriceDollars))
                 .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                 .ForMember(dest => dest.TotalPriceDollars, opt => opt.MapFrom(src => src.TotalPriceDollars))
                 .ForMember(dest => dest.ProductVolumeLitres, opt => opt.MapFrom(src => src.ProductVolumeLitres))
                 .ForMember(dest => dest.ProductSizeVariation, opt => opt.MapFrom(src => src.ProductSizeVariation));
        }

        // more mappings here later (user, cashier, admin, manager, transaction, etc.)
    }
}
