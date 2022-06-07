using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using HealthCareSystem.Core;
using HealthCareSystem.Core.Scripts.Repository;
using HealthCareSystem.Core.Users.Patients.Model;
using HealthCareSystem.Core.Users.Model;
using HealthCareSystem.Core.Examinations.Model;
using HealthCareSystem.Core.Users.Doctors.Model;
using HealthCareSystem.Core.Rooms.Model;
using HealthCareSystem.Core.Rooms.Repository;
using HealthCareSystem.Core.Rooms.DynamicEqipmentRequests.Model;
using HealthCareSystem.Core.Rooms.HospitalEquipment.RoomHasEquipment.Model;

namespace HealthCareSystem.Core.Users.Secretaries.Repository
{
    class SecretaryRepository
    {
        public DataTable Patients { get; set; }
        public OleDbConnection Connection { get; set; }
        private RoomRepository _RoomRepository { get; set; }

        public SecretaryRepository()
        {
            try
            {
                Connection = new OleDbConnection();

                Connection.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=../../Data/HCDb.mdb;
                Persist Security Info=False;";

                Connection.Open();

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
            _RoomRepository = new RoomRepository();
        }

        public List<string> GetSecretaryId(string userID)
        {
            var query = "SELECT id FROM Secretaries WHERE user_id = " + userID + "";
            return DatabaseCommander.ExecuteReaderQueries(query, Connection);
        }


    }
}