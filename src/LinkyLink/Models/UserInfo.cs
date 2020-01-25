namespace LinkyLink.Models
{
    public struct UserInfo
    {
        public static UserInfo Empty = new UserInfo("", "");
        //X-MS-CLIENT-PRINCIPAL-IDP
        public string IDProvider { get; }
        //http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress
        public string HashedID { get; }
        public UserInfo(string provider, string hashedID)
        {
            IDProvider = provider;
            HashedID = hashedID;
        }
    }
}