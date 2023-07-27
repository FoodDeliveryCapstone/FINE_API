using FirebaseAdmin.Auth;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public class FireBaseService
    {
        public static string GetUserIdFromHeaderToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jsonToken = handler.ReadToken(accessToken);
            }
            catch (Exception)
            {
                return null;
            }
            var tokenS = handler.ReadToken(accessToken) as JwtSecurityToken;
            var claims = tokenS.Claims;
            var id = claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value.ToString();
            return id;
        }
        public async static Task<UserRecord> GetUserRecordByIdToken(string idToken)
        {
            try
            {
                var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                FirebaseToken decodedToken = await auth.VerifyIdTokenAsync(idToken);
                UserRecord userRecord = await auth.GetUserAsync(decodedToken.Uid);
                return userRecord;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
