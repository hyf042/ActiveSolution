using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SeasideResearch.LibCurlNet;
using System.Text.RegularExpressions;
using System.Collections;

namespace ActiveSolution
{
    class CurlWrapper
    {
        public static string CookieName = "TmpCookie";

        public static bool Init()
        {
            if (Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL) != CURLcode.CURLE_OK)
                return false;
            return true;
        }

        // 写数据到文件
        public static Int32 WriteData(Byte[] buf, Int32 size, Int32 nmemb,
              Object extraData)
        {
            StringBuilder builder = extraData as StringBuilder;
            builder.Append(System.Text.Encoding.UTF8.GetString(buf));
            return size * nmemb;
        }

        // 对页面进行POST提交
        public static bool Post(string url, Dictionary<string, object> data, out string content)
        {
            try
            {
                string post = "";
                foreach (KeyValuePair<string, object> pair in data)
                    post += pair.Key + "=" + pair.Value.ToString() + "&";
                post.Trim('&');

                StringBuilder builder = new StringBuilder();
                Easy easy = new Easy();
                easy.SetOpt(CURLoption.CURLOPT_URL, url);
                easy.SetOpt(CURLoption.CURLOPT_COOKIEFILE, CookieName);
                easy.SetOpt(CURLoption.CURLOPT_COOKIEJAR, CookieName);
                easy.SetOpt(CURLoption.CURLOPT_POST, 1);
                easy.SetOpt(CURLoption.CURLOPT_POSTFIELDS, post);
                easy.SetOpt(CURLoption.CURLOPT_FOLLOWLOCATION, 1);
                easy.SetOpt(CURLoption.CURLOPT_WRITEFUNCTION, new Easy.WriteFunction(WriteData));
                easy.SetOpt(CURLoption.CURLOPT_USERAGENT, "GMActivate-gnet/0.1");
                easy.SetOpt(CURLoption.CURLOPT_WRITEDATA, builder);

                bool ret = (easy.Perform() == CURLcode.CURLE_OK);

                easy.Cleanup();

                content = builder.ToString();
                return ret;
            }
            catch
            {
                content = "";
                return false;
            }
        }
    }
}
