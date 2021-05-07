﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;
using DomainModels=Bit.Core.Models;

namespace Bit.Core.Entities
{
    public class Organization :  DomainModels.IKey<Guid>,IEntityCreated,IEntityUpdated
    {

        public Guid Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }        
        
        public bool Enabled { get; set; } = true;
        public string ApiKey { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreationDate { get; private set; } = DateTime.UtcNow;
        public DateTime RevisionDate { get; private set; } = DateTime.UtcNow;

        public ICollection<Cipher> Ciphers { get; set; }
/*        
        [IgnoreMap]
        public JsonDocument TwoFactorProvidersJson
        {
            get => _twoFactorProvidersJson;
            set
            {
                TwoFactorProviders = value?.ToString();
                _twoFactorProvidersJson = value;
            }
        }
*/        
    }

    public class OrganizationMapperProfile : Profile
    {
        public OrganizationMapperProfile()
        {
            CreateMap<DomainModels.Organization, Organization>().ReverseMap();
            CreateMap<Organization, DomainModels.Data.OrganizationUserOrganizationDetails>()
            .ForMember(d=>d.OrganizationId, opt=>opt.MapFrom(s=>s.Id))
            ;
        }
    }
}
