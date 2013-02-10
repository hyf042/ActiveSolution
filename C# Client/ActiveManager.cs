using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SeasideResearch.LibCurlNet;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Cryptography;

namespace ActiveSolution
{
    internal class HardwareInfo
    {
        public static string GetHostName()
        {
            return System.Net.Dns.GetHostName();
        }

        public static string GetMachineCode()
        {
            return GetHostName();
        }
    }

    internal class AsyncQuery
    {
        public enum StatusEnum { Idle, Requesting, Complete };
       
        public delegate void OnCompleteCallback();
        public event OnCompleteCallback onComplete;
        public delegate ActiveManager.ReplyCode OnTaskDelegate();
        public OnTaskDelegate taskDelegate;

        protected ActiveManager.ReplyCode reply = ActiveManager.ReplyCode.Query_Idle;
        protected StatusEnum status = StatusEnum.Idle;
        protected Thread task = null;

        public ActiveManager.ReplyCode Reply { get { return reply; } }

        public AsyncQuery(OnTaskDelegate task = null, OnCompleteCallback complete = null)
        {
            this.taskDelegate = task;
            this.onComplete += complete;

            this.task = new Thread(OnTask);
        }

        public virtual void Start()
        {
            if (status == StatusEnum.Requesting)
                return;

            if (task != null)
            {
                status = StatusEnum.Requesting;
                task.Start();
            }
        }

        public void Join()
        {
            if (status == StatusEnum.Requesting)
            {
                task.Join();
            }
        }

        public void Stop()
        {
            if (status == StatusEnum.Requesting)
            {
                task.Abort();
                status = StatusEnum.Idle;
            }
        }

        private void OnTask()
        {
            OnLogic();

            status = StatusEnum.Complete;
            if (onComplete != null)
                onComplete();
        }

        protected virtual void OnLogic()
        {
            if (taskDelegate != null)
                reply = taskDelegate();
        }
    };

    class ActiveManager
    {
        public enum ReplyCode { Actived_Yes, Actived_No, Query_Failed, Actvied_Successful, Actived_Fail, Actived_FileError, Query_Waiting, Query_Idle };
        public enum StatusEnum { Uninitailized, Unvalidated, Validating, Idle, Requesting };
        public delegate void OnRequestCompleteDelegate();

        private StatusEnum status = StatusEnum.Uninitailized;
        private const string QueryAddress = "http://localhost";
        private const string Query_CheckActive = "index.php?type=4";
        private const string Query_Activate = "index.php?type=5";
        private const string Query_InActivate = "index.php?type=8";
        private const string Query_ValidateActive = "index.php?type=7";

        private static readonly string ConfigFile = Path.Combine(Environment.CurrentDirectory, "tmp");
        private bool isActived;
        private string code;
        private string game;
        private string machine;
        private AsyncQuery query = null;
        private ActiveManager.ReplyCode reply;

        public OnRequestCompleteDelegate onRequestComplete = null;
        public StatusEnum Status { get { return status; } }
        public bool IsActived { get { return isActived; } }
        public string Code { get { return code; } }
        public string Game { get { return game; } }
        public string MachineCode { get { return machine; } }

        public ActiveManager.ReplyCode Reply
        {
            get
            {
                if (status == StatusEnum.Idle)
                    return reply;
                else if (status == StatusEnum.Requesting)
                    return ActiveManager.ReplyCode.Query_Waiting;
                else
                    return ActiveManager.ReplyCode.Query_Idle;
            }
        }
        #region Singleton
        private static ActiveManager instance = null;
        public static ActiveManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ActiveManager();
                return instance;
            }
        }
        #endregion

        #region Public Interface
        public bool Init()
        {
            if (status != StatusEnum.Uninitailized)
                return false;
            if (!CurlWrapper.Init())
                return false;

            status = StatusEnum.Unvalidated;
            return true;
        }

        public void Release()
        {
            Curl.GlobalCleanup();

            status = StatusEnum.Uninitailized;
        }

        public void InitValidate(bool isAsync = false)
        {
            if (status != StatusEnum.Idle && status != StatusEnum.Unvalidated)
                return;

            ValidationCheck(isAsync);
        }

        public bool CheckActivedAsync(string code)
        {
            if (status != StatusEnum.Idle)
                return false;

            status = StatusEnum.Requesting;
            query = new AsyncQuery(() => { return CheckActived(code); }, QueryComplete_Default);
            query.Start();

            return true;
        }

        public bool ActivateAsync(string code, string game)
        {
            if (status != StatusEnum.Idle)
                return false;

            status = StatusEnum.Requesting;
            query = new AsyncQuery(() => { return Activate(code, game); }, QueryComplete_Default);
            query.Start();

            return true;
        }

        public bool InactivateAsync()
        {
            if (status != StatusEnum.Idle)
                return false;

            status = StatusEnum.Requesting;
            query = new AsyncQuery(() => { return Inactivate(); }, QueryComplete_Default);
            query.Start();

            return true;
        }

        public ActiveManager.ReplyCode CheckActived(string code)
        {
            Dictionary<string, object> postData = new Dictionary<string, object>();
            postData["code"] = code;

            string content;
            if (!CurlWrapper.Post(QueryAddress + "/" + Query_CheckActive, postData, out content))
                return ActiveManager.ReplyCode.Query_Failed;

            if (content.IndexOf("Yes, it's actived") >= 0)
                return ActiveManager.ReplyCode.Actived_Yes;
            else
                return ActiveManager.ReplyCode.Actived_No;
        }

        public ActiveManager.ReplyCode Activate(string code, string game)
        {
            string machine = GetMachineCode();

            Dictionary<string, object> postData = new Dictionary<string, object>();
            postData["code"] = code;
            postData["game"] = game;
            postData["machine"] = machine;

            string content;
            if (!CurlWrapper.Post(QueryAddress + "/" + Query_Activate, postData, out content))
                return ActiveManager.ReplyCode.Query_Failed;

            if (content.IndexOf("Activate Successful!") >= 0)
            {
                if (WriteValidated(code, game, machine))
                {
                    this.game = game;
                    this.code = code;
                    isActived = true;
                    return ActiveManager.ReplyCode.Actvied_Successful;
                }
                else
                    return ActiveManager.ReplyCode.Actived_FileError;
            }
            else
                return ActiveManager.ReplyCode.Actived_Fail;
        }

        public ActiveManager.ReplyCode Inactivate()
        {
            if (!isActived)
                return ActiveManager.ReplyCode.Query_Failed;

            Dictionary<string, object> postData = new Dictionary<string, object>();
            postData["code"] = code;
            postData["game"] = game;

            string content;
            if (!CurlWrapper.Post(QueryAddress + "/" + Query_InActivate, postData, out content))
                return ActiveManager.ReplyCode.Actived_Fail;

            if (content.IndexOf("inactivate:true") >= 0)
            {
                game = code = "";
                isActived = false;
                File.Delete(ConfigFile);
                return ActiveManager.ReplyCode.Actvied_Successful;
            }
            else
                return ActiveManager.ReplyCode.Actived_Fail;
        }

        #endregion

        #region Private Funcs
        private byte[] ReadFileData(string filename)
        {
            FileStream sr = File.OpenRead(filename);

            byte[] ret = new byte[sr.Length];
            sr.Read(ret, 0, (int)sr.Length);
            sr.Close();

            return ret;
        }
        private void WriteDataFile(string filename, byte[] bytes)
        {
            BinaryWriter sw = new BinaryWriter(File.OpenWrite(filename));
            sw.Write(bytes);
            sw.Close();
        }

        private void ValidationCheck(bool isAsync = false)
        {
            status = StatusEnum.Idle;
            reply = ActiveManager.ReplyCode.Actived_No;
            isActived = false;

            if (!File.Exists(ConfigFile))
            {
                return;
            }

            Dictionary<string, object> jsonDict = null;

            try
            {
                string content = Encoding.UTF8.GetString(Codec.RFA.Instance.Decode(ReadFileData(ConfigFile)));
                jsonDict = MiniJSON.Json.Deserialize(content) as Dictionary<string, object>;
            }
            catch
            {
                File.Delete(ConfigFile);
                return;
            }

            if (jsonDict == null ||
                !jsonDict.ContainsKey("actived") || !jsonDict.ContainsKey("machine") ||
                !jsonDict.ContainsKey("code") || !jsonDict.ContainsKey("game"))
            {
                File.Delete(ConfigFile);
                return;
            }

            if (Convert.ToInt32(jsonDict["actived"]) == 0)
            {
                File.Delete(ConfigFile);
                return;
            }

            string machine = GetMachineCode();
            if (machine != (string)jsonDict["machine"])
            {
                File.Delete(ConfigFile);
                return;
            }

            string game = (string)jsonDict["game"];
            string code = (string)jsonDict["code"];

            if (isAsync)
            {
                status = StatusEnum.Validating;

                query = new AsyncQuery(() => { return ValidateActived(code, game, machine); }, QueryComplete_Default);
                query.Start();
            }
            else
            {
                reply = ValidateActived(code, game, machine);
            }

            return;
        }
        private ActiveManager.ReplyCode ValidateActived(string code, string game, string machine)
        {
            Dictionary<string, object> postData = new Dictionary<string, object>();
            postData["code"] = code;
            postData["game"] = game;
            postData["machine"] = machine;

            string content;
            ActiveManager.ReplyCode ret = ActiveManager.ReplyCode.Query_Waiting;

            if (!CurlWrapper.Post(QueryAddress + "/" + Query_ValidateActive, postData, out content))
                ret = ActiveManager.ReplyCode.Query_Failed;

            if (content.IndexOf("actived:true") >= 0)
                ret = ActiveManager.ReplyCode.Actived_Yes;

            if (ret != ActiveManager.ReplyCode.Query_Waiting)
            {
                this.game = game;
                this.code = code;
                this.machine = machine;
                isActived = true;

                return ret;
            }
            else
            {
                File.Delete(ConfigFile);
                isActived = false;
                return ActiveManager.ReplyCode.Actived_No;
            }
        }
        private bool WriteValidated(string code, string game, string machine)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            dict["actived"] = 1;
            dict["code"] = code;
            dict["game"] = game;
            dict["machine"] = machine;

            string content = MiniJSON.Json.Serialize(dict);

            byte[] raw = Encoding.UTF8.GetBytes(content);
            WriteDataFile(ConfigFile, Codec.RFA.Instance.Encode(raw));

            if (File.Exists(ConfigFile))
                return true;

            return false;
        }
        #endregion

        #region Static Utility
        public static string GetMachineCode()
        {
            return GetMD5Hash(HardwareInfo.GetMachineCode());
        }
        public static string GetMD5Hash(string code)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] res = md5.ComputeHash(Encoding.UTF8.GetBytes(code));
            StringBuilder builder = new StringBuilder();
            foreach (byte data in res)
                builder.Append(data.ToString("x2"));

            return builder.ToString();
        }
        #endregion

        #region Async Callback Funcs
        private void QueryComplete_Default()
        {
            reply = query.Reply;

            status = StatusEnum.Idle;
        }
        #endregion
    }
}
