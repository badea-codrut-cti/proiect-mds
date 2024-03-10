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
        public static int MAX_STAKE = 30;
        public static int MIN_STAKE = 1;
        public List<Validator> Validators { get; private set; }
        public Block Block { get; private set; }

        public ValidatorSelector(List<Validator> validators, Block block)
        {
            if (validators.Count == 0)
                throw new ArgumentException("Validator list is empty.");

            if (validators.Count > 100 / MIN_STAKE)
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

        }
    }
}
