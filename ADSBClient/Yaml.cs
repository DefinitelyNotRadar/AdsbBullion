using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeResolvers;

namespace ADSBClientLib
{
    public class Yaml
    {
        public LocalProperties YamlLoad()
        {
            string text = "";
            try
            {
                using (StreamReader sr = new StreamReader("LocalProperties.yaml", System.Text.Encoding.Default))
                {
                    text = sr.ReadToEnd();
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var deserializer = new DeserializerBuilder().Build();

            var localProperties = new LocalProperties();
            try
            {
                localProperties = deserializer.Deserialize<LocalProperties>(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (localProperties == null)
            {
                localProperties = GenerateDefaultLocalProperties();
                YamlSave(localProperties);
            }
            return localProperties;
        }

        private LocalProperties GenerateDefaultLocalProperties()
        {
            var localProperties = new LocalProperties();
            localProperties.IPtcpMicroAdsb = "192.168.0.11";
            localProperties.TcpPortMicroAdsb = 30005;

            localProperties.IPudpLocal = "192.168.1.11";
            localProperties.IPudpRemote = "225.0.0.37";

            localProperties.UdpPortLocal = 16000;
            localProperties.UdpPortRemote = 16001;

            localProperties.NameApplication = "grozaAdsb";
            //localProperties.endPoint = "192.168.1.108:8302";
            localProperties.endPoint = "127.0.0.1:30051";

            localProperties.WritingSerialNumber = "";
            localProperties.IsWritingQuery = true;

            ///for sending adsb data to lib user by udp
            localProperties.UDPAdsbReceiverPort = 16002;
            localProperties.UDPAdsbReceiverIp = "127.0.0.1";
            localProperties.IsSend2UDPAdsbReceiver = false;

            localProperties.Hemisphere = "Northern";

            //reference coordinates
            localProperties.RefLatitude = 53.889725;
            localProperties.RefLongitude = 28.032442;
            

            return localProperties;
        }

        public T YamlLoad<T>(string NameDotYaml) where T : new()
        {
            string text = "";
            try
            {
                using (StreamReader sr = new StreamReader(NameDotYaml, System.Text.Encoding.Default))
                {
                    text = sr.ReadToEnd();
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var deserializer = new DeserializerBuilder().Build();

            var t = new T();
            try
            {
                t = deserializer.Deserialize<T>(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return t;
        }

        public void YamlSave(LocalProperties localProperties)
        {
            try
            {
                var serializer = new SerializerBuilder().WithTypeResolver(new StaticTypeResolver()).Build();

                var yaml = serializer.Serialize(localProperties);
                string key = "DependencyObjectType";
                int pos = yaml.IndexOf(key);
                if (pos != -1)
                    yaml = yaml.Replace(yaml.Substring(pos), "");

                using (StreamWriter sw = new StreamWriter("LocalProperties.yaml", false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(yaml);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void YamlSave<T>(T t, string NameDotYaml) where T : new()
        {
            try
            {
                var serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve).Build();
                var yaml = serializer.Serialize(t);

                using (StreamWriter sw = new StreamWriter(NameDotYaml, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(yaml);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
