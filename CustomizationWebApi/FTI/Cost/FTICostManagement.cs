using CustomizationWebApi.Client.Cost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Server;

namespace CustomizationWebApi
{
    public class FTICostManagement
    {
        #region Properties
        public IEntityManager EntityManager { get; }
        public Logger Logger { get; }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="logger"></param>
        public FTICostManagement(IEntityManager entityManager, Logger logger)
        {
            EntityManager = entityManager;
            Logger = logger;
        }

        /// <summary>
        /// Process Cost Soap Client
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ProcessSoapClient(string orderNumber)
        {
            zLIMS_ACT_COSTS_GETClient client = new();

            Z_QM_ACT_COSTS request = new Z_QM_ACT_COSTS
            {
                IV_PLANT = orderNumber
            };

            Logger.Error($"Sending request to BAPI for plant: {orderNumber}");
            var response = await client.Z_QM_ACT_COSTSAsync(request);

            if (response == null)
            {
                Logger.Error("Received null response from BAPI");
                throw new InvalidOperationException("BAPI returned null response");
            }

            var rateQuery = EntityManager.CreateQuery<FtiRateCardBase>();
            var rates = EntityManager.Select(rateQuery);

            var itemsFromSap = response.Z_QM_ACT_COSTSResponse.ET_COSTS;

            foreach (var item in itemsFromSap)
            {
                var existingRecord = rates.Cast<FtiRateCardBase>().Where(x => x.Identity == item.LSTAR).FirstOrDefault();

                if (existingRecord != null)
                {
                    Logger.Error($"Updating existing rate card for {item.LSTAR}");

                    MapRateCard(item, existingRecord);
                    existingRecord.ModifiedOn = DateTime.Now;

                    DeleteExistingRateEntriesByRateCard(existingRecord);
                    CreatePriceEntries(existingRecord, item);
                }
                else
                {
                    Logger.Error($"Creating new rate card for {item.LSTAR}");

                    var newRateCard = CreateNewRateCard(item);

                    CreatePriceEntries(newRateCard, item);

                    EntityManager.Transaction.Add(newRateCard);
                }
            }
            //Should it commit each entry one by one or all at once?
            EntityManager.Commit();

            Logger.Info($"Successfully retrieved order details for order: {orderNumber}");
            Logger.Debug($"BAPI Response: {response}");
        }

        private void DeleteExistingRateEntriesByRateCard(FtiRateCardBase existingRecord)
        {
            Logger.Debug($"Clearing out existing records for rate card {existingRecord.Name}");

            foreach (IEntity entry in existingRecord.FtiRateEntries)
            {
                EntityManager.Delete(entry);
            }

            EntityManager.Commit();
        }

        private FtiRateCardBase CreateNewRateCard(ZQM_S_ACT_COSTS item)
        {
            FtiRateCardBase newRateCard = EntityManager.CreateEntity<FtiRateCardBase>();

            MapRateCard(item, newRateCard);

            return newRateCard;
        }

        private void MapRateCard(ZQM_S_ACT_COSTS? item, FtiRateCardBase newRateCard)
        {
            newRateCard.Identity = item.LSTAR;
            newRateCard.Unit = item.UNIT;

            if (item.LSTAR?.Length > 0)
            {
                var firstTwoCharactersOfRate = item.LSTAR?.Substring(0, 2);
                Logger.Error($"First two characters of Rate are {firstTwoCharactersOfRate}");

                switch (firstTwoCharactersOfRate)
                {
                    case "MR":
                        newRateCard.SetType(PhraseFtiRateT.PhraseIdMR);
                        break;
                    case "PR":
                        newRateCard.SetType(PhraseFtiRateT.PhraseIdPR);
                        break;
                    default:
                        newRateCard.SetType(PhraseFtiRateT.PhraseIdIBLV);
                        break;
                }
            }
        }

        private void CreatePriceEntries(FtiRateCardBase rateCard, ZQM_S_ACT_COSTS item)
        {
            Logger.Error($"Creating new price entries for {rateCard.Name}");

            int month = 1;
            MapRateEntry(rateCard, item.GJAHR, item.TKG001, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG002, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG003, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG004, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG005, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG006, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG007, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG008, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG009, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG010, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG011, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG012, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG013, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG014, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG015, month++);
            MapRateEntry(rateCard, item.GJAHR, item.TKG016, month++);
        }

        private void MapRateEntry(FtiRateCardBase rateCard, string gJAHR, decimal priceValue, int month)
        {
            FtiRateEntryBase rateEntry = EntityManager.CreateEntity<FtiRateEntryBase>();

            rateEntry.Ftiratecard = rateCard;
            int.TryParse(gJAHR, out int year);
            rateEntry.Year = year;
            rateEntry.Month = month;
            rateEntry.Price = decimal.ToDouble(priceValue);

            EntityManager.Transaction.Add(rateEntry);
        }
    }
}
