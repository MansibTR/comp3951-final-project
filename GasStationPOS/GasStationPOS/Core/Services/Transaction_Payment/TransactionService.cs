﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GasStationPOS.Core.Data.Database.Json.JsonToModelDTOs;
//using GasStationPOS.Core.Data.Database.Json.JsonToModelDTOs.Transaction;
using GasStationPOS.Core.Data.Models.ProductModels;
using GasStationPOS.Core.Data.Models.TransactionModels;
using GasStationPOS.Core.Data.Models.UserModels;
using GasStationPOS.Core.Data.Repositories.TransactionRepository;
using GasStationPOS.Core.Database.Json;
using GasStationPOS.UI.MainFormDataSchemas.DTOs;

namespace GasStationPOS.Core.Services.Transaction_Payment
{
    /// <summary>
    /// Service for transaction operations (create, delete all, get previous) - methods called from main form.
    /// Uses transation repository for data source access.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        readonly ITransactionRepository transactionRepository;

        /// <summary>
        /// The current transaction number used when creating new transactions - private, SHOULD NOT BE SET BY USER
        /// </summary>
        private int currentTransactionNumber;

        /// <summary>
        /// The latest transaction number
        /// </summary>
        public int LatestTransactionNumber
        {
            get => Math.Max(currentTransactionNumber - 1, TransactionConstants.INITIAL_TRANSACTION_NUM); 
        }

        public TransactionService(ITransactionRepository transactionRepository)
        {
            this.transactionRepository = transactionRepository;
            this.currentTransactionNumber = TransactionConstants.INITIAL_TRANSACTION_NUM;
        }

        public async Task<bool> CreateTransactionAsync(PaymentMethod paymentMethod, decimal totalAmountDollars, decimal amountTenderedDollars, IEnumerable<ProductDTO> products)
        {
            // validate parameters
            if (!ValidateTransactionPaymentFields(totalAmountDollars, amountTenderedDollars, products)) {
                return false; // unsuccessful transaction
            }

            Transaction transaction = new Transaction
            {
                TransactionNumber               = currentTransactionNumber,
                TransactionFuelProductItems     = new List<TransactionFuelProductItem>(),
                TransactionRetailProductItems   = new List<TransactionRetailProductItem>(),
                PaymentMethod                   = paymentMethod,
                TotalAmountDollars              = totalAmountDollars,
                ChangeDollars                   = amountTenderedDollars - totalAmountDollars,
                TransactionDateTime             = DateTime.Now
            };

            // Go through the list of products passed in, and add the correct type of product to the list based on the Product subclass
            foreach (ProductDTO productDTO in products)
            {
                if (productDTO is RetailProductDTO)
                {
                    RetailProductDTO rpDTO = (RetailProductDTO)productDTO;

                    // Convert dto -> model via automapper
                    RetailProduct retailProduct = Program.GlobalMapper.Map<RetailProduct>(rpDTO);

                    // Create transaction retail product item list entry
                    TransactionRetailProductItem rpTransactionEntry = new TransactionRetailProductItem
                    {
                        RetailProduct           = retailProduct,
                        Quantity                = rpDTO.Quantity,
                        TotalItemPriceDollars   = rpDTO.TotalPriceDollars
                    };
                    
                    // Add to TransactionRetailProductItems
                    transaction.TransactionRetailProductItems.Add(rpTransactionEntry);
                }
                else if (productDTO is FuelProductDTO)
                {
                    FuelProductDTO fpDTO = (FuelProductDTO)productDTO;

                    // Convert to the dto -> model via automapper
                    FuelProduct fuelProduct = Program.GlobalMapper.Map<FuelProduct>(fpDTO);

                    // Create transaction fuel product item list entry
                    TransactionFuelProductItem fpTransactionEntry = new TransactionFuelProductItem
                    {
                        FuelProduct             = fuelProduct,
                        Quantity                = fpDTO.Quantity,
                        TotalItemPriceDollars   = fpDTO.TotalPriceDollars
                    };

                    // Add to TransactionRetailProductItems
                    transaction.TransactionFuelProductItems.Add(fpTransactionEntry);
                }
            }

            // validate the created transaction - calling validate using datavalidation class
            //================================ Jason TODO ================================

            // create transaction asyncronously
            await transactionRepository.Create(transaction);

            // update current transaction number
            currentTransactionNumber++;

            return true; // successful transaction
        }

        /// <summary>
        /// Deletes all transactions stored in the data source. Returns true if successful, otherwise false.
        /// </summary>
        public bool DeleteAllTransactions()
        {
            try
            {
                transactionRepository.DeleteAll();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a collection of all the product dtos in the previous transaction (within the bounds of the first and current latest transaction numbers)
        /// Returns an empty collection if there were no previous transactions, or transaction was not found.
        /// This method is asyncronous (to avoid blocking during data source load operations from transactionRepository get all method)
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ProductDTO>> GetTransactionProductListAsync(int transactionNumber)
        {
            // ensure transaction number is within the valid range - from the first t.num to the current latest t.num
            int validTransactionNumber = GetChosenTransactionNumberWithinBounds(transactionNumber);

            // Initialize products view dtos as empty list
            List<ProductDTO> productDTOList = new List<ProductDTO>();

            // Get all transactions dtos from data source
            IEnumerable<TransactionDatabaseDTO> transactionDTOList = await transactionRepository.GetAll();

            // Find the transaction db dto object with transaction id == the previous transaction id
            TransactionDatabaseDTO previousTransactionDbDto = transactionDTOList.FirstOrDefault(transactionDbDTO => transactionDbDTO.TransactionNumber == validTransactionNumber);

            // if transaction was not found
            if (previousTransactionDbDto == null)
            {
                return productDTOList;
            }

            // Load fuel and retail product data (as view dtos) into productDTOList from previous transaction database dto object

            // get RETAIL product view dtos from the list of transaction retail products
            // and add to productDTOList
            foreach (TransactionRetailProductItem transactionRPItem in previousTransactionDbDto.TransactionRetailProductItems)
            {
                RetailProduct retailProduct = transactionRPItem.RetailProduct;

                // Convert model -> dto via automapper
                RetailProductDTO retailProductDTO = Program.GlobalMapper.Map<RetailProductDTO>(retailProduct);

                // add unique fields not stored in the model
                retailProductDTO.TotalPriceDollars = transactionRPItem.TotalItemPriceDollars;
                retailProductDTO.Quantity = transactionRPItem.Quantity;

                productDTOList.Add(retailProductDTO);
            }
            // get FUEL product view dtos from the list of transaction fuel products
            // and add to productDTOList
            foreach (TransactionFuelProductItem transactionFPItem in previousTransactionDbDto.TransactionFuelProductItems)
            {
                FuelProduct fuelProduct = transactionFPItem.FuelProduct;

                // Convert model -> dto via automapper
                FuelProductDTO fuelProductDTO = Program.GlobalMapper.Map<FuelProductDTO>(fuelProduct);

                // add unique fields not stored in the model
                fuelProductDTO.TotalPriceDollars = transactionFPItem.TotalItemPriceDollars;
                fuelProductDTO.Quantity = transactionFPItem.Quantity;

                productDTOList.Add(fuelProductDTO);
            }

            return productDTOList;
        }

        /// <summary>
        /// Returns a valid chosen transaction number used when indexing through previous transactions (within
        /// </summary>
        public int GetChosenTransactionNumberWithinBounds(int chosenTransactionNum)
        {
            int minLatestTransactionNumber;

            if (currentTransactionNumber == TransactionConstants.INITIAL_TRANSACTION_NUM)
            {
                minLatestTransactionNumber = TransactionConstants.INITIAL_TRANSACTION_NUM;
            }
            else
            {
                minLatestTransactionNumber = currentTransactionNumber - 1;
            }

            // ensure transaction number is within the valid range - from the first t.num to the current latest t.num
            int validTransactionNumberUpperBound = Math.Min(chosenTransactionNum, minLatestTransactionNumber); // ensure not past the latest t.num
            int validTransactionNumber = Math.Max(TransactionConstants.INITIAL_TRANSACTION_NUM, validTransactionNumberUpperBound); // ensure not past the initial t.num

            return validTransactionNumber;
        }

        ///// <summary>
        ///// Returns a collection of all the product dtos in the FIRST transaction.
        ///// Returns an empty collection if there were no previous transactions, or transaction was not found.
        ///// This method is asyncronous (to avoid blocking during data source load operations from transactionRepository get all method)
        ///// </summary>
        //public async Task<Tuple<IEnumerable<ProductDTO>, int>> GetFirstTransactionProductListAsync()
        //{
        //    return await GetProductsForTransactionNumberAsync(TransactionConstants.INITIAL_TRANSACTION_NUM);
        //}

        ///// <summary>
        ///// Returns a collection of all the product dtos in the NEXT transaction. 
        ///// If at the end of the transaction list, will return the prodcut list for the same transaction.
        ///// Returns an empty collection if there were no previous transactions, or transaction was not found.
        ///// This method is asyncronous (to avoid blocking during data source load operations from transactionRepository get all method)
        ///// </summary>
        //public async Task<Tuple<IEnumerable<ProductDTO>, int>> GetNextTransactionProductListAsync()
        //{
        //    // if current chosen transaction number has not reached the latest transaction number
        //    if (currentlyChosenTransactionNum < currentTransactionNumber)
        //    {
        //        currentlyChosenTransactionNum++;
        //    }
        //   return await GetProductsForTransactionNumberAsync(currentlyChosenTransactionNum);
        //}

        ///// <summary>
        ///// Returns a collection of all the product dtos in the PREVIOUS transaction. 
        ///// If at the beginning of the transaction list, will return the prodcut list for the same transaction.
        ///// Returns an empty collection if there were no previous transactions, or transaction was not found.
        ///// This method is asyncronous (to avoid blocking during data source load operations from transactionRepository get all method)
        ///// </summary>
        //public async Task<Tuple<IEnumerable<ProductDTO>, int>> GetPreviousTransactionProductListAsync()
        //{
        //    // if current chosen transaction number has not reached the first transaction number
        //    if (currentlyChosenTransactionNum > TransactionConstants.INITIAL_TRANSACTION_NUM)
        //    {
        //        currentlyChosenTransactionNum--;
        //    }
        //    return await GetProductsForTransactionNumberAsync(currentlyChosenTransactionNum);
        //}

        private static bool ValidateTransactionPaymentFields(decimal totalAmountDollars, decimal amountTenderedDollars, IEnumerable<ProductDTO> products)
        {
            if (products == null) return false;

            decimal totalAmountDollarsProductsTest = 0.0m;

            foreach (ProductDTO product in products)
            {
                if (product == null) return false;
                totalAmountDollarsProductsTest += product.TotalPriceDollars;
            }

            if (totalAmountDollarsProductsTest != totalAmountDollars) return false;

            if (amountTenderedDollars < totalAmountDollars) return false;

            return true;
        }
    }
}
