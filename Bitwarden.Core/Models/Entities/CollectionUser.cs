﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;

namespace Bit.Core.Models
{
    public class CollectionAssigned 
    {        
        virtual public int Id {get;set;}
        public Guid CollectionId {get;set;}
        public Guid UserId {get;set;}
        public virtual bool ReadOnly {get;set;}
        public virtual bool HidePasswords {get;set;}
    }
}
