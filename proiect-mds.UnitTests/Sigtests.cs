using proiect_mds.blockchain;
using proiect_mds.blockchain.exception;
using proiect_mds.blockchain.impl;
using proiect_mds.daemon;
using System.Net.Sockets;
using System.Text;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class KeyBasedSignChecks
    {
        [TestMethod]
        public void SignTransaction()
        {
            const string privateKeyString = "MHQCAQEEICYLBvaZ2/NWEOhSJGWF+yNUIBgebDEIoEl1i0mHUUbtoAcGBSuBBAAKoUQDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYFUmCdRNYOy0/Hac0s4hit57N2QryIK0PSxEIMAG2tkb4hj5uYpLWM2w==";
            var sender = new WalletId(Encoding.UTF8.GetBytes("0123456789abcdef"));
            var receiver = new WalletId(Encoding.UTF8.GetBytes("0123456789ABCDEF"));

            var key = new PrivateKey(privateKeyString);
            Assert.IsNotNull(key);

            var transaction = key.SignTransaction(sender, receiver, 100, new DateTime(0xFFEEDDCCAA01));
            Assert.IsNotNull(transaction);

            const string publicKeyString = "MFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYFUmCdRNYOy0/Hac0s4hit57N2QryIK0PSxEIMAG2tkb4hj5uYpLWM2w==";
            var pubKey = new PublicKey(publicKeyString);
            Assert.IsTrue(pubKey.ValidateTransaction(transaction));
        }
        
        [TestMethod]
        [ExpectedException(typeof(PrivateKeyException))]
        public void ErrorOnInvalidKey()
        {
            const string privateKeyString = "MHQCAQEEIUUbtoAcGBSuBBAAKoUQDQgAECk6OstRglNkGmV/jTjV2k0apW+ViPuYFUmCdRNYOy0/Hac0s4hit57N2QryIK0PSxEIMAG2tkb4hj5uYpLWM2w==";
            var key = new PrivateKey(privateKeyString);
        }
    }
}