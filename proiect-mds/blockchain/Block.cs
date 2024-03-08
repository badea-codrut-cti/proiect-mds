using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain {
    internal class Block {
        public int Index { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string PreviousHash { get; private set; }
        public string ValidatorId { get; private set; }
        public string SenderId { get; private set; }
        public string ReceiverId { get; private set; }
        public int Amount { get; private set; }
        public string Hash { get; private set; }

        public Block(int index, DateTime timestamp, string previousHash, string validatorId, string senderId, string receiverId, int amount)
        {
            Index = index;
            Timestamp = timestamp;
            PreviousHash = previousHash;
            ValidatorId = validatorId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Amount = amount;
            Hash = CalculateHash();
        }

        private string CalculateHash() {
            using (var sha256 = SHA256.Create()) {
                string dataToHash = $"{Index}{Timestamp}{PreviousHash}{ValidatorId}{SenderId}{ReceiverId}{Amount}";
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToHexString(hashedBytes);
            }
        }
    }
}
