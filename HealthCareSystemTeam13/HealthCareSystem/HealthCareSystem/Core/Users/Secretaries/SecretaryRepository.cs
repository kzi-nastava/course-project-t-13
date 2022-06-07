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
        public DataTable BlockedPatients { get; set; }
        public DataTable RequestsPatients { get; set; }
        public DataTable ReferralLetters { get; set; }
        public DataTable ClosestExaminations { get; set; }
        public DataTable EquipmentInWarehouse { get; set; }
        public DataTable DynamicEquipment { get; set; }
        public DataTable TransferDynamicEquipment { get; set; }
        public DataTable DaysOffRequests { get; set; }
        public OleDbConnection Connection { get; set; }
        public RoomRepository RoomRep { get; set; }

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
            RoomRep = new RoomRepository();
        }

        private void FillTable(DataTable table, string query)
        {
            using (var cmd = new OleDbCommand(query, Connection))
            {
                OleDbDataReader reader = cmd.ExecuteReader();
                table.Load(reader);
            }
        }

        public void PullPatients()
        {
            Patients = new DataTable();
            var query = "select Patients.ID, Patients.firstName, Patients.lastName, Users.usrnm, Users.pass from Patients INNER JOIN Users ON Users.id = patients.user_id";
            FillTable(Patients, query);
        }

        public void PullBlockedPatients()
        {
            BlockedPatients = new DataTable();
            string blockedPatientsQuery = "select BlockedPatients.id, Patients.FirstName as FirstName, Patients.LastName as LastName, BlockedPatients.id_secretary as BlockedBy from BlockedPatients inner join Patients on Patients.id = BlockedPatients.id_patient";
            FillTable(BlockedPatients, blockedPatientsQuery);
        }

        public void PullExaminationRequests()
        {
            RequestsPatients = new DataTable();
            var query = "select * from PatientEditRequest";
            FillTable(RequestsPatients, query);
        }

        public void PullReferralLetters()
        {
            ReferralLetters = new DataTable();
            var query = "select * from ReferralLetter";
            FillTable(ReferralLetters, query);
        }

        public void PullClosestExaminations(string roomId)
        {
            ClosestExaminations = new DataTable();
            DateTime fromDateTime = DateTime.Now;
            DateTime toDateTime = DateTime.Now.AddHours(2);
            var query = "select * from (select * from Examination WHERE dateOf >#" + fromDateTime.ToString() + "# and dateOf <#" + toDateTime + "# and id_room  = " + roomId + " and isUrgent = 0 ORDER BY dateOf) where rownum < 5 ";
            FillTable(ClosestExaminations, query);
        }

        public void PullClosestExaminations(DoctorSpeciality speciality)
        {
            ClosestExaminations = new DataTable();
            DateTime fromDateTime = DateTime.Now;
            DateTime toDateTime = DateTime.Now.AddHours(2);
            var query = "select * from (select * from Examination WHERE dateOf > #" + fromDateTime.ToString() + "# and dateOf < #" + toDateTime.ToString() + "# and id_doctor in (SELECT id FROM doctors WHERE speciality = " + speciality.ToString() + " ) ORDER BY dateOf) where rownum < 5 ";
            FillTable(ClosestExaminations, query);
        }

        public void PullEquipmentInWarehouse()
        {
            EquipmentInWarehouse = new DataTable();
            var warehouseId = GetWarehouseId();
            var query = "select * from Equipment where type = 'Dynamic' and id not in (select id_equipment from RoomHasEquipment where id_room = " + warehouseId + " and amount > 0)";
            FillTable(EquipmentInWarehouse, query);
        }

        public void PullDynamicEquipment()
        {
            DynamicEquipment = new DataTable();
            var query = "select * from RoomHasEquipment where amount < 6";
            FillTable(DynamicEquipment, query);
        }
        
        public void PullTransferDynamicEquipment(int equipmentId)
        {
            TransferDynamicEquipment = new DataTable();
            var query = "select * from RoomHasEquipment where id_equipment = " + equipmentId.ToString() + "";
            FillTable(TransferDynamicEquipment, query);
        }

        public void PullDaysOffRequests()
        {
            DaysOffRequests = new DataTable();
            var query = "select * from DoctorRequestDaysOf df where df.id not in (select mdf.id_request from ManagementOfDaysOfRequests mdf)";
            FillTable(DaysOffRequests, query);
        }

        public void InsertSingleUser(User user)
        {
            var query = "INSERT INTO users(usrnm, pass, role) VALUES(@usrnm, @pass, @role)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@usrnm", user.Username);
                cmd.Parameters.AddWithValue("@pass", user.Password);
                cmd.Parameters.AddWithValue("@role", user.Role.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> GetUserId(string username)
        {
            var query = "SELECT id FROM Users WHERE usrnm = '" + username + "'";
            return DatabaseCommander.ExecuteReaderQueries(query, Connection);
        }

        public List<string> GetPatientId(string userID)
        {
            var query = "SELECT id FROM Patients WHERE user_id = " + userID + "";
            return DatabaseCommander.ExecuteReaderQueries(query, Connection);
        }

        public List<string> GetSecretaryId(string userID)
        {
            var query = "SELECT id FROM Secretaries WHERE user_id = " + userID + "";
            return DatabaseCommander.ExecuteReaderQueries(query, Connection);
        }
        public int GetSecretaryIdFromUsername(string username)
        {
            var query = "SELECT sc.id FROM Secretaries as sc inner join Users as us on sc.user_id = us.id where us.usrnm =  '" + username + "'";
            return Convert.ToInt32(DatabaseCommander.ExecuteReaderQueries(query, Connection)[0]);
        }
        public int GetWarehouseId()
        {
            var query = "SELECT id FROM Rooms WHERE type = 'Warehouse'";
            return Convert.ToInt32(DatabaseCommander.ExecuteReaderQueries(query, Connection)[0]);
        }

        public void InsertSinglePatient(Patient patient)
        {
            var query = "INSERT INTO Patients(firstName, lastName, user_id, isBlocked) VALUES(@firstName, @LastName, @user_id, @isBlocked)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@firstName", patient.FirstName);
                cmd.Parameters.AddWithValue("@LastName", patient.LastName);
                cmd.Parameters.AddWithValue("@user_id", patient.UserId);
                cmd.Parameters.AddWithValue("@isBlocked", patient.IsBlocked);
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertSingleMedicalRecord(MedicalRecord medicalRecord)
        {
            var query = "INSERT INTO MedicalRecord(id_patient, height, weight) VALUES(@id_patient, @height, @weight)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_patient", medicalRecord.IdPatient);
                cmd.Parameters.AddWithValue("@height", medicalRecord.Height);
                cmd.Parameters.AddWithValue("@weight", medicalRecord.Weight);
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertSingleExamination(Examination examination)
        {
            var query = "INSERT INTO Examination(id_doctor, id_patient, isEdited, isCancelled, isFinished, dateOf, typeOfExamination, isUrgent, id_room, duration) " +
                "VALUES(@id_doctor, @id_patient, @isEdited, @isCancelled, @isFinished, @dateOf, @typeOfExamination, @isUrgent, @id_room, @duration)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_doctor", examination.IdDoctor);
                cmd.Parameters.AddWithValue("@id_patient", examination.IdPatient);
                cmd.Parameters.AddWithValue("@isEdited", examination.IsEdited);
                cmd.Parameters.AddWithValue("@isCancelled", examination.IsCancelled);
                cmd.Parameters.AddWithValue("@isFinished", examination.IsFinished);
                cmd.Parameters.AddWithValue("@dateOf", examination.DateOf.ToString());
                cmd.Parameters.AddWithValue("@typeOfExamination", examination.TypeOfExamination.ToString());
                cmd.Parameters.AddWithValue("@isUrgent", examination.IsUrgent);
                cmd.Parameters.AddWithValue("@id_room", examination.IdRoom);
                cmd.Parameters.AddWithValue("@duration", examination.Duration);
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertSingleDynamicEquipmentRequest(DynamicEquipmentRequest request)
        {
            var query = "INSERT INTO RequestForDinamicEquipment(id_equipment, amount, dateOf, id_secretary) VALUES(@id_equipment, @amount, @dateOf, @id_secretary)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_equipment", request.EquipmentId);
                cmd.Parameters.AddWithValue("@amount", request.Quantity);
                cmd.Parameters.AddWithValue("@dateOf", request.Date.ToString());
                cmd.Parameters.AddWithValue("@id_secretary", request.SecretaryId);
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertSingleBlockedPatient(BlockedPatient blockedPatient)
        {
            var query = "INSERT INTO BlockedPatients(id_patient, id_secretary, dateOf) VALUES(@id_patient, @id_secretary, @dateOf)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_patient", blockedPatient.PatientID);
                cmd.Parameters.AddWithValue("@id_secretary", blockedPatient.SecretaryID);
                cmd.Parameters.AddWithValue("@dateOf", blockedPatient.DateOf);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateSigleDynamicEquipment(DynamicEquipmentRequest request)
        {
            int warehouseId = GetWarehouseId();
            var amounts = DatabaseCommander.ExecuteReaderQueries("select amount from RoomHasEquipment " +
               "where id_room = " + warehouseId + " and id_equipment = " + request.EquipmentId, Connection);
           string query;
            if (amounts.Count != 0)
            {
                query = "Update RoomHasEquipment SET amount = " + (Convert.ToInt32(amounts[0]) + request.Quantity) + " WHERE id_room =" + warehouseId + " and id_equipment = " + request.EquipmentId;
            }
            else
            {
                query = "Insert into RoomHasEquipment(id_room, id_equipment, amount) values(" + GetWarehouseId() + ", " + request.EquipmentId + ", " + request.Quantity + ")";
            }
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateSigleDynamicEquipment(int amount, int roomHasEquipmentID)
        {
            var query = "Update RoomHasEquipment SET amount = amount - " + amount + " WHERE id = " + roomHasEquipmentID.ToString();
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateSigleDynamicEquipment(int amount, RoomHasEquipment roomHasEquipment)
        {
            var query = "Update RoomHasEquipment SET amount = amount + " + amount + " WHERE id = " + roomHasEquipment.Id.ToString();
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSinglePatient(string patientID)
        {
            var query = "SELECT user_id FROM Patients WHERE id = " + Convert.ToInt32(patientID) + "";
            string userID = DatabaseCommander.ExecuteReaderQueries(query, Connection)[0];

            query = "DELETE from Users WHERE id = " + userID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSingleBlockedPatient(string blockedPatientID)
        {
            var query = "SELECT id_patient FROM BlockedPatients WHERE id = " + blockedPatientID + "";
            string patientID = DatabaseCommander.ExecuteReaderQueries(query, Connection)[0];
            
            query = "DELETE from BlockedPatients WHERE ID = " + blockedPatientID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }

            query = "UPDATE Patients SET isBlocked = 0 WHERE ID = " + patientID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSinglePatientRequest(string requestID)
        {
            var query = "DELETE from PatientEditRequest WHERE ID = " + Convert.ToInt32(requestID) + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSingleExamination(string requestID)
        {
            var query = "SELECT id_examination from PatientEditRequest WHERE id = " + requestID + "";
            string examinationID = DatabaseCommander.ExecuteReaderQueries(query, Connection)[0];
            
            query = "UPDATE Examination SET iscancelled = 1 WHERE ID = " + examinationID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSingleReferralLetter(string letterID)
        {
            var query = "DELETE from ReferralLetter WHERE id = " + Convert.ToInt32(letterID) + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSingleDynamicEquipmentRequest(int requestID)
        {
            var query = "DELETE from RequestForDinamicEquipment WHERE id = " + requestID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public Dictionary<string, string> GetPatientInformation(string patientID)
        {
            var query = "select Patients.firstName, Patients.lastName, Users.usrnm, Users.pass from Patients INNER JOIN Users ON users.id = patients.user_id WHERE patients.id = " + patientID + "";
            Dictionary<string, string> row = new Dictionary<string, string>();

            OleDbCommand cmd = DatabaseCommander.GetCommand(query, Connection);

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                row["firstName"] = reader["firstName"].ToString();
                row["lastName"] = reader["lastName"].ToString();
                row["usrnm"] = reader["usrnm"].ToString();
                row["pass"] = reader["pass"].ToString();
            }
            return row;
        }

        public void UpdatePatient(string patientID, string username, string password, string name, string lastname)
        {
            var query = "SELECT user_id FROM Patients WHERE id = " + patientID + "";
            string userID = DatabaseCommander.ExecuteReaderQueries(query, Connection)[0];

            query = "UPDATE Users SET usrnm = @usrnm, pass = @pass WHERE ID = @userID";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@usrnm", username);
                cmd.Parameters.AddWithValue("@pass", password);
                cmd.Parameters.AddWithValue("@userID", userID);
                cmd.ExecuteNonQuery();
            }

            query = "UPDATE Patients SET firstname = @firstname, lastname = @astname WHERE ID = @patientID";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@firstname", name);
                cmd.Parameters.AddWithValue("@lastname", lastname);
                cmd.Parameters.AddWithValue("@patientID", patientID);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateExamination(string requestID)
        {
            Dictionary<string, string> information = GetPatientRequest(requestID);
            var query = "UPDATE Examination SET id_doctor = @id_doctor, dateOf = @dateOf, id_room = @id_room WHERE ID = @examination_id";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_doctor", information["doctor_id"]);
                cmd.Parameters.AddWithValue("@dateOf", information["dateTimeOfExamination"]);
                cmd.Parameters.AddWithValue("@id_room", information["room_id"]);
                cmd.Parameters.AddWithValue("@examination_id", information["examination_id"]);
                cmd.ExecuteNonQuery();
            }
            query = "UPDATE Examination SET isedited = 1 WHERE ID = " + information["examination_id"] + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void BlockSinglePatient(string patientID, string username)
        {
            string userID = GetUserId(username)[0];
            var query = "SELECT ID FROM Secretaries WHERE user_id = " + userID + "";
            string secretaryID = DatabaseCommander.ExecuteReaderQueries(query, Connection)[0];
            
            query = "UPDATE Patients SET isBlocked = 1 WHERE ID = " + patientID + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
            
            BlockedPatient blockedPatient = new BlockedPatient(Convert.ToInt32(patientID), Convert.ToInt32(secretaryID), new DateTime(2022, 10, 12));
            InsertSingleBlockedPatient(blockedPatient);
        }

        public bool IsRequestChanged(string requestID)
        {
            var query = "SELECT isChanged FROM PatientEditRequest WHERE id = " + requestID + "";
            return Convert.ToBoolean(DatabaseCommander.ExecuteReaderQueries(query, Connection)[0]);
        }

        public Dictionary<string, string> GetPatientRequest(string requestID)
        {
            var query = "SELECT id_examination, id_doctor, dateTimeOfExamination, id_room FROM PatientEditRequest WHERE id = " + requestID + "";
            Dictionary<string, string> row = new Dictionary<string, string>();
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Connection;
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = query;

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                row["examination_id"] = reader["id_examination"].ToString();
                row["doctor_id"] = reader["id_doctor"].ToString();
                row["dateTimeOfExamination"] = reader["dateTimeOfExamination"].ToString();
                row["room_id"] = reader["id_room"].ToString();
            }
            return row;
        }

        public List<string> GetSpecialistsIds(DoctorSpeciality speciality)
        {
            var query = "SELECT ID FROM Doctors WHERE speciality = '" + speciality.ToString() + "'";
            return DatabaseCommander.ExecuteReaderQueries(query, Connection);
        }

        public List<Examination> GetDoctorsEximanitonsInNextTwoHours(string doctorId)
        {
            List<Examination> examinations = new List<Examination>();
            DateTime fromDateTime = DateTime.Now;
            DateTime toDateTime = DateTime.Now.AddHours(2);
            var query = "SELECT id, id_doctor, id_patient, isEdited, isCancelled, isFinished, dateOf, typeOfExamination, isUrgent, id_room, duration FROM Examination " +
                "WHERE dateOf > #" + fromDateTime.ToString() + "# and dateof < #" + toDateTime.ToString() + "# and id_doctor  = " + doctorId + "";

            Dictionary<string, string> row = new Dictionary<string, string>();

            OleDbCommand cmd = DatabaseCommander.GetCommand(query, Connection);

            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                examinations.Add(GetExaminationFromReader(reader));
            }

            return examinations;
        }

        public List<Examination> GetRoomEximanitonsFromTo(DateTime fromDateTime, DateTime toDateTime, string roomId)
        {
            List<Examination> examinations = new List<Examination>();
            var query = "SELECT id, id_doctor, id_patient, isEdited, isCancelled, isFinished, dateOf, typeOfExamination, isUrgent, id_room, duration FROM Examination " +
                "WHERE dateOf BETWEEN (" + fromDateTime + ", " + toDateTime + ") and id_room  = " + roomId + "";
            Dictionary<string, string> row = new Dictionary<string, string>();
            OleDbCommand cmd = DatabaseCommander.GetCommand(query, Connection);

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                examinations.Add(GetExaminationFromReader(reader));
            }

            return examinations;
        }

        private static Examination GetExaminationFromReader(OleDbDataReader reader)
        {
            return new Examination((int)reader["id"], (int)reader["id_doctor"], (int)reader["id_patient"], (bool)reader["isEdited"], (bool)reader["isCancelled"], (bool)reader["isFinished"], (DateTime)reader["dateOf"],
                                                             (TypeOfExamination)reader["typeOfExamination"], (bool)reader["isUrgent"], (int)reader["id_room"], (int)reader["duration"]);
        }

        private static DynamicEquipmentRequest GetDynamicEquipmentRequestsFromReader(OleDbDataReader reader)
        {
            return new DynamicEquipmentRequest((int)reader["id_equipment"], (int)reader["amount"], (DateTime)reader["dateOf"], (int)reader["id_secretary"], (int)reader["id"]);
        }

        public List<DynamicEquipmentRequest> GetDeliveredDynamicEquipmentRequest()
        {
            List<DynamicEquipmentRequest> requests = new List<DynamicEquipmentRequest>();
            var query = "SELECT id, id_equipment, amount, dateOf, id_secretary FROM RequestForDinamicEquipment where dateOf < #" + (DateTime.Now.AddDays(-1)).ToString() + "#";
            OleDbCommand cmd = DatabaseCommander.GetCommand(query, Connection);

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                requests.Add(GetDynamicEquipmentRequestsFromReader(reader));
            }
            
            return requests;
        }

        public List<Examination> GetDoctorsExamiantions(DateTime from, DateTime to, string doctorId)
        {
            List<Examination> examinations = new List<Examination>();
            var query = "SELECT id, id_doctor, id_patient, isEdited, isCancelled, isFinished, dateOf, typeOfExamination, isUrgent, id_room, duration FROM Examination " +
                "WHERE dateOf > #" + from.ToString() + "# and dateOf < #" + to.ToString() + "# and id_doctor  = " + doctorId + "";
            
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Connection;
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = query;

            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                examinations.Add(GetExaminationFromReader(reader));
            }

            return examinations;
        }

        public Tuple<string, DateTime> AvailableExamination(DoctorSpeciality speciality, int duration)
        {
            List<string> doctorsID = GetSpecialistsIds(speciality);
            Tuple<string, DateTime> closestTimeAndDoctor = new Tuple<string, DateTime>("none", DateTime.Now.AddHours(2)); 
            
            foreach (string doctorID in doctorsID)
            {
                List<Examination> examinations = GetDoctorsEximanitonsInNextTwoHours(doctorID);
                examinations = examinations.OrderBy(examination => examination.DateOf).ToList();
                TimeSpan timeSpan = TimeSpan.FromSeconds(0);
                
                if(examinations.Count() > 0)
                    timeSpan = DateTime.Now - examinations[0].DateOf;
                if (timeSpan.TotalMinutes <= duration)
                {
                    closestTimeAndDoctor = new Tuple<string, DateTime>(doctorID, DateTime.Now);
                }

                for (int index = 0; index < examinations.Count() - 1; index++)
                {
                    timeSpan = examinations[index + 1].DateOf - examinations[index].DateOf;
                    if (timeSpan.TotalMinutes <= duration)
                    {
                        if(examinations[index].DateOf.AddMinutes(examinations[index].Duration) < closestTimeAndDoctor.Item2 )
                        {
                            closestTimeAndDoctor = new Tuple<string, DateTime>(doctorID, examinations[index].DateOf.AddMinutes(examinations[index].Duration));
                        }
                    }
                }

                if(examinations.Count() > 0)
                    timeSpan = DateTime.Now.AddHours(2) - examinations[examinations.Count - 1].DateOf;
                if (timeSpan.TotalMinutes <= duration)
                {
                    if (examinations[examinations.Count - 1].DateOf.AddMinutes(examinations[examinations.Count - 1].Duration) < closestTimeAndDoctor.Item2)
                    {
                        closestTimeAndDoctor = new Tuple<string, DateTime>(doctorID, examinations[examinations.Count - 1].DateOf.AddMinutes(examinations[examinations.Count - 1].Duration));
                    }
                }
            }
            return closestTimeAndDoctor;
        }

        public int GetAvailableRoom(DateTime dateTime, int duration)
        {
            if(duration > 15)
            {
                List<string> roomsId = RoomRep.GetOperationRooms();
                foreach(string roomId in roomsId)
                {
                    if(IsRoomAvailable(roomId, dateTime, duration))
                    {
                        return Convert.ToInt32(roomId);
                    }    
                }
            } 
            else
            {
                List<string> roomsId = RoomRep.GetExaminationRooms();
                foreach (string roomId in roomsId)
                {
                    if (IsRoomAvailable(roomId, dateTime, duration))
                    {
                        return Convert.ToInt32(roomId);
                    }
                }
            }

            return 0;
        }

        public bool IsRoomAvailable(string roomId, DateTime dateTime, int duration)
        {
            DateTime fromDateTime = DateTime.Now;
            DateTime toDateTime = DateTime.Now.AddHours(2);
            List<Examination> examinations = GetRoomEximanitonsFromTo(fromDateTime, toDateTime, roomId);
            
            foreach(Examination examination in examinations)
            {
                if (examination.DateOf < dateTime.AddMinutes(duration) && examination.DateOf > dateTime)
                    return false;
            }
            return true;
        }

        void MoveExamination(int id, DateTime dateTime)
        {
            var query = "UPDATE Examination SET dateOf = " + dateTime + " WHERE ID = " + id + "";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void MoveDoctorsExaminations(DateTime from, DateTime to, int doctorId)
        {
            List<Examination> movingExaminations = GetDoctorsExamiantions(from, to, doctorId.ToString());
            foreach (Examination movingExamination in movingExaminations)
            {
                List<Examination> examinations = GetDoctorsExamiantions(to, DateTime.Now.AddDays(30), (movingExamination.IdDoctor).ToString());
                examinations = examinations.OrderBy(examination => examination.DateOf).ToList();
                bool found = false;

                for (int index = 0; index < examinations.Count() - 1; index++)
                {
                    TimeSpan timeSpan = examinations[index + 1].DateOf - examinations[index].DateOf.AddMinutes(examinations[index].Duration);
                    if (timeSpan.TotalMinutes <= movingExamination.Duration )
                    {
                        MoveExamination(examinations[index].Id, examinations[index].DateOf.AddMinutes(examinations[index].Duration));
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    MoveExamination(examinations[examinations.Count() - 1].Id, examinations[examinations.Count() - 1].DateOf.AddMinutes(examinations[examinations.Count() - 1].Duration));
                }
            }
        }
    
        public void CheckDynamicEquipmentRequests()
        {
            List<DynamicEquipmentRequest> requests = GetDeliveredDynamicEquipmentRequest();
            foreach (DynamicEquipmentRequest request in requests)
            {
                UpdateSigleDynamicEquipment(request);
                DeleteSingleDynamicEquipmentRequest(request.ID);
            }
        }

        public void ManageDaysOffRequest(string username, int requestId, bool approved, string comment = "")
        {
            var query = "INSERT INTO ManagementOfDaysOfRequests(id_request, id_secretary, isApproved, comment) VALUES(@id_request, @id_secretary, @isapproved, @comment)";
            using (var cmd = new OleDbCommand(query, Connection))
            {
                cmd.Parameters.AddWithValue("@id_request", requestId);
                cmd.Parameters.AddWithValue("@id_secretary",GetSecretaryIdFromUsername(username));
                cmd.Parameters.AddWithValue("@isapproved", approved);
                cmd.Parameters.AddWithValue("@comment", comment);
                cmd.ExecuteNonQuery();
            }
        }

    }
}