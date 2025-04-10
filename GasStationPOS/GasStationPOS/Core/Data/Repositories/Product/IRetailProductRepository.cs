﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GasStationPOS.Core.Data.Models.ProductModels;

namespace GasStationPOS.Core.Data.Repositories.Product
{
    public interface IRetailProductRepository
    {
        //        void Create(RetailProduct retailProductModel);
        //        void Update(RetailProduct retailProductModel);
        //        void Delete(int productId);
        IEnumerable<RetailProduct> GetAll();

 //       IEnumerable<RetailProduct> GetByValue(string fuelProductName); // for searching product info
    }
}
