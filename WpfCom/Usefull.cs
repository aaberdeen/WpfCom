
namespace WpfApplication1
{
    public class Usefull
    {
        /// <summary>
        /// Checks that a valid IP adderss has been entered
        /// </summary>
        /// <param name="server"></param>
        /// <returns>bool</returns>
        public static bool ValidIP(string server)
        {
            string[] parts = server.Split('.');
            if (parts.Length < 4)
            {
                return false; // not a IPv4 string in X.X.X.X format
            }
            else
            {
                foreach (string part in parts)
                {
                    byte checkPart = 0;
                    if (!byte.TryParse(part, out checkPart))
                    {
                        // not a valid IPv4 string in X.X.X.X format
                        return false;
                    }
                }
                // it is a valid IPv4 string in X.X.X.X format
                return true;
            }
        }
    }
}
