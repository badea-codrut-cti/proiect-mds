using proiect_mds.blockchain;
using proiect_mds.blockchain.exception;
using System.Text;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class KeyBasedSignChecks
    {
        [TestMethod]
        public void SignTransaction()
        {
            const string privateKeyString = "-----BEGIN EC PRIVATE KEY-----\r\nMHQCAQEEICYLBvaZ2/NWEOhSJGWF+yNUIBgebDEIoEl1i0mHUUbtoAcGBSuBBAAK\r\noUQDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYFUmCdRNYOy0/Hac0s4hit57N2\r\nQryIK0PSxEIMAG2tkb4hj5uYpLWM2w==\r\n-----END EC PRIVATE KEY-----\r\n";
            var sender = new WalletId(Encoding.UTF8.GetBytes("0123456789abcdef"));
            var receiver = new WalletId(Encoding.UTF8.GetBytes("0123456789ABCDEF"));
            var key = new PrivateKey(privateKeyString);
            var transaction = key.SignTransaction(sender, receiver, 100, new DateTime(0xFFEEDDCCAA01));

            Assert.IsNotNull(transaction);

            const string publicKeyString = "-----BEGIN PUBLIC KEY-----\r\nMFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYF\r\nUmCdRNYOy0/Hac0s4hit57N2QryIK0PSxEIMAG2tkb4hj5uYpLWM2w==\r\n-----END PUBLIC KEY-----\r\n";
            var pubKey = new PublicKey(publicKeyString);
            Assert.IsTrue(pubKey.ValidateTransaction(transaction));
        }

        [TestMethod]
        [ExpectedException(typeof(PrivateKeyException))]
        public void ErrorOnInvalidKey()
        {
            const string privateKeyString = "-----BEGIN EC PRIVATE KEY-----\r\nMHQCAQEEIUUbtoAcGBSuBBAAK\r\noUQDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYFUmCdRNYOy0/Hac0s4hit57N2\r\nQryIK0PSxEIMAG2tkb4hj5uYpLWM2w==\r\n-----END EC PRIVATE KEY-----\r\n";
            var key = new PrivateKey(privateKeyString);
        }
    }
    
}