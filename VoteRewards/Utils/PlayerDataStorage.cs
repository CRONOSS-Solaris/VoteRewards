using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VoteRewards.Utils
{
    public class PlayerDataStorage
    {
        private static PlayerDataStorage _instance;
        private static readonly object _lock = new object();
        private readonly string _dataFilePath;

        private PlayerDataStorage(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        public static PlayerDataStorage GetInstance(string dataFilePath)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new PlayerDataStorage(dataFilePath);
                    }
                }
            }
            return _instance;
        }

        public void SavePlayerData(XDocument doc)
        {
            lock (_lock)
            {
                doc.Save(_dataFilePath);
            }
        }

        public XDocument LoadPlayerData()
        {
            lock (_lock)
            {
                return XDocument.Load(_dataFilePath);
            }
        }
    }

}
