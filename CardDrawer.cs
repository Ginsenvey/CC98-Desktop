
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace CC98.Kernel
{
    public static class CardDrawer
    {
        public static async Task<string> DrawACard(string rule)
        {
            string url = "https://card.cc98.org/api/draw/1";
            var res = await CCloginservice.vpn.PostAsync(url, null);
            return await ValidationHelper.AutoResponse(res);
        }
        public static async Task<string> Stat()
        {
            string url = "https://card.cc98.org/api/collection/stat";
            return await RequestSender.SimpleRequest(url);
        }
        public static async Task<string> DestroyAll()
        {
            string url = "https://card.cc98.org/api/collection/all-rest";
            try
            {
                var r = await CCloginservice.vpn.DeleteAsync(url);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return "1";
                }
            }
            catch
            {

            }
            return "0";
        }
    }
    
    
}