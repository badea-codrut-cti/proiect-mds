using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    internal class Validator
    {
        public WalletId WalletId { get; private set; }
        public UInt32 Stake { get; private set; }

        public Validator(WalletId walletId, UInt32 stake)
        {
            this.WalletId = walletId;
            this.Stake = stake;
        }
    }

    internal class ValidatorSelector
    {
        public static uint MAX_STAKE = 30;
        public static uint MIN_STAKE = 1;
        public static uint MAX_VALIDATOR_COUNT = 100000;
        public List<Validator> Validators { get; private set; }
        public Block Block { get; private set; }

        public ValidatorSelector(List<Validator> validators, Block block)
        {
            if (validators.Count == 0)
                throw new ArgumentException("Validator list is empty.");

            if (validators.Count > MAX_VALIDATOR_COUNT / MIN_STAKE)
                throw new ArgumentException("Validator list is too large.");

            Validators = validators;
            Block = block;
        }

        public UInt64 GetTotalStakes()
        {
            UInt64 totalStakes = 0;
            foreach (Validator validator in Validators)
            {
                totalStakes += validator.Stake;
            }

            return totalStakes;
        }

        public Validator GetPickedValidator()
        {
            UInt64 totalStakes = GetTotalStakes(), weighedStakes = 0;
            foreach (Validator validator in Validators)
            {
                UInt64 stake = ((100 * validator.Stake) / totalStakes);
                weighedStakes += Math.Max(stake > MAX_STAKE ? MAX_STAKE : stake, MIN_STAKE);
            }

            var random = new SecureRandom();
            byte[] randomBytes = new byte[sizeof(UInt64)];
            random.NextBytes(randomBytes);
            UInt64 pickedStake = BitConverter.ToUInt64(randomBytes, 0) % (weighedStakes + 1);

            weighedStakes = 0;

            foreach (Validator validator in Validators)
            {
                UInt64 stake = ((100 * validator.Stake) / totalStakes);
                weighedStakes += Math.Max(stake > MAX_STAKE ? MAX_STAKE : stake, MIN_STAKE);

                if (weighedStakes >= pickedStake)
                    return validator;
            }

            throw new ApplicationException("Error in validator picking.");
        }
    }

    internal class BlockValidator {
        private readonly Blockchain blockchain;
        public BlockValidator(Blockchain blockchain)
        {
            this.blockchain = blockchain;
        }
        
        bool ValidateBlock(Block block)
        {
            if (block.Index != blockchain.GetLatestBlock().Index + 1)
                return false;

            var transactionOffset = new Dictionary<WalletId, Int64>();

            foreach(var transaction in block.Transactions)
            {
                if (transaction.Timestamp > block.Timestamp)
                    return false;

                var balanceBeforeBlock = blockchain.GetWalletBalance(transaction.Sender);
                if (balanceBeforeBlock == null)
                    return false;

                Int64 senderOffset = 0;
                transactionOffset.TryGetValue(transaction.Sender, out senderOffset);

                if ((long)balanceBeforeBlock + senderOffset < (long)transaction.Amount)
                    return false;

                var pubKey = blockchain.GetKeyFromWalletId(transaction.Sender);
                if (pubKey == null) 
                    return false;

                if (!pubKey.ValidateTransaction(transaction))
                    return false;

                transactionOffset.Add(transaction.Sender, senderOffset - (long)transaction.Amount);

                Int64 receiverOffset = 0;
                transactionOffset.TryGetValue(transaction.Receiver, out receiverOffset);
                transactionOffset.Add(transaction.Receiver, receiverOffset + (long)transaction.Amount);
            }

            return true;
        }
    }
}
