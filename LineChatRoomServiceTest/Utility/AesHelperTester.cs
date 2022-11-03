using LineChatRoomService.Utility;
using System.Security.Cryptography;

namespace LineChatRoomServiceTest.Utility
{
    public class AesHelperTester
    {

        [Fact]
        public void Test_AesHelper_Symmetric_Encryption_Is_Work()
        {
            var aes = Aes.Create();

            var testInput = "asdfghjklwertyuio2345678";
            var meta = AesHelper.EncryptString(aes, testInput);
            var output = AesHelper.DecryptString(aes, meta);

            Assert.Equal(testInput, output);
        }
    }
}
