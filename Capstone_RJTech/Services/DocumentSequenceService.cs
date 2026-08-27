using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services
{
    public class DocumentSequenceService
    {
        private const string CheckoutSequence = "checkout";
        private readonly ApplicationDbContext _db;

        public DocumentSequenceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public int PeekNextDeliveryNumber(DateTime date)
        {
            string sequenceName = DeliverySequenceName(date);
            int storedValue = _db.DocumentSequences
                .AsNoTracking()
                .Where(item => item.sequence_name == sequenceName)
                .Select(item => item.last_value)
                .FirstOrDefault();

            return Math.Max(storedValue, FindLargestExistingDeliveryNumber(date)) + 1;
        }

        public int AllocateNextDeliveryNumber(DateTime date)
            => AllocateNext(DeliverySequenceName(date), FindLargestExistingDeliveryNumber(date));

        public int AllocateNextCheckoutNumber()
        {
            int largestExisting = _db.Checkouts
                .Select(checkout => (int?)checkout.CheckoutNumber)
                .Max() ?? 0;

            return AllocateNext(CheckoutSequence, largestExisting);
        }

        private int AllocateNext(string sequenceName, int minimumValue)
        {
            var sequence = _db.DocumentSequences
                .SingleOrDefault(item => item.sequence_name == sequenceName);

            if (sequence == null)
            {
                sequence = new DocumentSequence
                {
                    sequence_name = sequenceName,
                    last_value = minimumValue
                };
                _db.DocumentSequences.Add(sequence);
            }
            else if (sequence.last_value < minimumValue)
            {
                sequence.last_value = minimumValue;
            }

            sequence.last_value = checked(sequence.last_value + 1);
            return sequence.last_value;
        }

        private int FindLargestExistingDeliveryNumber(DateTime date)
        {
            string prefix = $"BATCH-{date:yyyyMMdd}-";
            return _db.Deliveries
                .AsNoTracking()
                .Where(delivery => delivery.batch_ID.StartsWith(prefix))
                .Select(delivery => delivery.batch_ID)
                .AsEnumerable()
                .Select(batchId => int.TryParse(batchId[prefix.Length..], out int value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static string DeliverySequenceName(DateTime date)
            => $"delivery:{date:yyyyMMdd}";
    }
}
