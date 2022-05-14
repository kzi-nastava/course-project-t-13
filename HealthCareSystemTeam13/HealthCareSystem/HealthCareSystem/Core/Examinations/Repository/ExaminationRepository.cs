﻿using HealthCareSystem.Core.Examinations.Model;
using HealthCareSystem.Core.Rooms.Repository;
using HealthCareSystem.Core.Users.Doctors.Model;
using HealthCareSystem.Core.Users.Doctors.Repository;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCareSystem.Core.Examinations.Repository
{
    class ExaminationRepository
    {
        OleDbConnection Connection;
        RoomRepository RoomRep;
        DoctorRepository DoctorRep;
        public ExaminationRepository()
        {
            try
            {
                Connection = new OleDbConnection();

                Connection.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=HCDb.mdb;
                Persist Security Info=False;";

                

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
            RoomRep = new RoomRepository();
            DoctorRep = new DoctorRepository();
        }
        public List<Examination> GetFinishedExaminations(int patientId)
        {
            if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

            List<Examination> examinations = new List<Examination>();

            OleDbCommand cmd = DatabaseHelpers.GetCommand("select * from Examination where id_patient = " + patientId + " and dateOf < #" + DateTime.Now.ToString() + "#", Connection);
            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                SetExaminationValues(examinations, reader);
            }

            return examinations;
        }
        public List<Examination> GetAllOtherExaminations(int currentExaminationId)
        {
            List<Examination> examinations = new List<Examination>();
            Connection.Open();

            OleDbCommand cmd = DatabaseHelpers.GetCommand("select * from Examination where not id = "+currentExaminationId+"", Connection);
            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                SetExaminationValues(examinations, reader);
            }
            Connection.Close();

            return examinations;
        }

        public List<Examination> GetAllExaminations()
        {
            List<Examination> examinations = new List<Examination>();
            Connection.Open();

            OleDbCommand cmd = DatabaseHelpers.GetCommand("select * from Examination", Connection);
            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                SetExaminationValues(examinations, reader);
            }
            Connection.Close();

            return examinations;
        }

        private static void SetExaminationValues(List<Examination> examinations, OleDbDataReader reader)
        {
            TypeOfExamination typeOfExamination;
            Enum.TryParse<TypeOfExamination>(reader["typeOfExamination"].ToString(), out typeOfExamination);

            examinations.Add(new Examination(
                Convert.ToInt32(reader["ID"]),
                Convert.ToInt32(reader["id_doctor"]),
                Convert.ToInt32(reader["id_patient"]),
                false,
                false,
                false,
                (DateTime)reader["dateOf"],
                typeOfExamination,
                false,
                Convert.ToInt32(reader["id_room"]),
                Convert.ToInt32(reader["duration"])
                ));
        }

        public DoctorAnamnesis GetDoctorAnamnesis(int examinationId)
        {
            if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

            string query = "select a.id_examination as ExaminationId, a.notice as Notice, a.conclusions as Conclusions, e.dateOf as DateOfExamination, d.firstName + ' ' + d.lastName as Doctor, d.speciality as Speciality from (Anamnesises a inner join Examination e on a.id_examination = e.id) inner join doctors d on e.id_doctor = d.id where e.id = " + examinationId + "";

            OleDbCommand cmd = DatabaseHelpers.GetCommand(query, Connection);
            DoctorAnamnesis anamnesis = null;

            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                anamnesis = SetDoctorAnamnesisValues(reader);
            }

            return anamnesis;
        }

        private static DoctorAnamnesis SetDoctorAnamnesisValues(OleDbDataReader reader)
        {
            return new DoctorAnamnesis(Convert.ToInt32(
                                                        reader["ExaminationId"]),
                                                        reader["Notice"].ToString(),
                                                        reader["Conclusions"].ToString(),
                                                        (DateTime)reader["DateOfExamination"],
                                                        reader["Doctor"].ToString(),
                                                        reader["Speciality"].ToString()
                                                        );
        }

        public Anamnesis GetAnamnesis(int examinationId)
        {
            if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

            OleDbCommand cmd = DatabaseHelpers.GetCommand("select * from Anamnesises where id_examination = " + examinationId + "", Connection);
            Anamnesis anamnesis = null;

            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                anamnesis = SetAnamnesisValues(reader);
            }

            return anamnesis;
        }

        private static Anamnesis SetAnamnesisValues(OleDbDataReader reader)
        {
            return new Anamnesis(Convert.ToInt32(reader["id_examination"]),
                                                        reader["notice"].ToString(),
                                                        reader["conclusions"].ToString(),
                                                        (DateTime)reader["dateOf"]);
        }


        public List<Examination> GetRecommendedExaminations(Doctor selectedDoctor, string startTime, string endTime, DateTime examinationFinalDate, bool isDoctorPriority)
        {
            List<Examination> examinations;

            // get taken appointments in criteria
            List<Examination> takenExaminations = GetTakenExaminations(selectedDoctor.ID, startTime, endTime, examinationFinalDate);

            // gets free examinations based on the taken ones
            examinations = GetFreeExaminations(selectedDoctor.ID, examinationFinalDate, startTime, endTime, isDoctorPriority, false, takenExaminations);

            if (examinations.Count() == 0)
            {
                examinations = GetFreeExaminations(selectedDoctor.ID, examinationFinalDate, startTime, endTime, isDoctorPriority, true, takenExaminations);

                if(examinations.Count() == 0)
                {
                    examinations = GetFreeExaminations(selectedDoctor.ID, examinationFinalDate, startTime, endTime, isDoctorPriority, false, takenExaminations, true);

                }
            }


            return examinations;
        }

        private List<Examination> GetFreeExaminations(int doctorId, DateTime examinationFinalDate, string startTime, string endTime, bool isDoctorPriority, bool isPriorityUsed, List<Examination> takenExaminations, bool isTopThree = false)
        {
            List<Examination> examinations = new List<Examination>();

            // setting dates and times
            DateTime startDate = Helpers.GetMergedDateTime(DateTime.Now.AddDays(1), startTime);
            DateTime endDate = Helpers.GetMergedDateTime(examinationFinalDate, endTime);
            int startHour = startDate.Hour;
            int startMinute = startDate.Minute;
            int endHour = endDate.Hour;
            int endMinute = endDate.Minute;
            int totalFoundExaminations = 0;
            int roomId;

            SetTimeDateValuesBasedOnPriorities(isDoctorPriority, isPriorityUsed, isTopThree, ref startDate, ref endDate, ref startHour, ref endHour);

            while (startDate.CompareTo(endDate) <= 0)
            {
                bool isExaminationFound = true;
                roomId = RoomRep.GetAvailableRoomId(startDate, takenExaminations);
                if (roomId != 0)
                {
                    CountValidExaminations(ref doctorId, isDoctorPriority, isPriorityUsed, takenExaminations, isTopThree, examinations, startDate, ref totalFoundExaminations, ref roomId, ref isExaminationFound);

                    if (isExaminationFound)
                    {
                        examinations.Add(new Examination(doctorId, startDate, TypeOfExamination.BasicExamination, roomId));
                        totalFoundExaminations++;
                        if (totalFoundExaminations > 4) break;
                    }
                }
                startDate = GetNewStartDate(startDate, startHour, startMinute, endHour, endMinute);
            }
            if (isTopThree) return examinations.GetRange(0, 3);

            return examinations;

        }

        private void CountValidExaminations(ref int doctorId, bool isDoctorPriority, bool isPriorityUsed, List<Examination> takenExaminations, bool isTopThree, List<Examination> examinations, DateTime startDate, ref int totalFoundExaminations, ref int roomId, ref bool isExaminationFound)
        {
            if (!isPriorityUsed || (isPriorityUsed && isDoctorPriority))
            {
                foreach (Examination takenExam in takenExaminations)
                {
                    if (!IsValidTimeAndDoctor(startDate, takenExam, doctorId))
                    {
                        isExaminationFound = false;
                        continue;
                    }
                    else totalFoundExaminations++;
                }
            }
            else if ((isPriorityUsed && !isDoctorPriority) || isTopThree)
            {
                Doctor availableDoctor = DoctorRep.GetAvailableDoctor(startDate, examinations);
                if (availableDoctor != null)
                {
                    doctorId = availableDoctor.ID;
                    roomId = RoomRep.GetAvailableRoomId(startDate, takenExaminations);
                    if (roomId > 0) { totalFoundExaminations++; }
                }
                else { isExaminationFound = false; }

            }
        }

        private static void SetTimeDateValuesBasedOnPriorities(bool isDoctorPriority, bool isPriorityUsed, bool isTopThree, ref DateTime startDate, ref DateTime endDate, ref int startHour, ref int endHour)
        {
            if (isPriorityUsed && isDoctorPriority)
            {
                startDate = startDate.AddHours(-4);
                endDate = endDate.AddDays(2).AddHours(4);
            }
            else if (isTopThree)
            {
                endDate = endDate.AddDays(2);
                startHour = startDate.AddHours(-2).Hour;
                endHour = endDate.AddHours(2).Hour;
            }
        }

        private static DateTime GetNewStartDate(DateTime startDate, int startHour, int startMinute, int endHour, int endMinute)
        {
            startDate = startDate.AddMinutes(15);
            if (startDate.Hour > endHour)
            {
                startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startHour, startMinute, 0).AddDays(1);
            }
            return startDate;
        }

        private bool IsDoctorEqual(int doctorId, Examination takenExamination)
        {
            return doctorId == takenExamination.IdDoctor;
        }
        private bool IsValidTime(DateTime startDate, Examination takenExamination)
        {
            TimeSpan difference = startDate.Subtract(takenExamination.DateOf);
            if (Math.Abs(difference.TotalMinutes) < 15) return false;
            return true;
        }
        private bool IsValidTimeAndDoctor(DateTime startDate, Examination takenExamination, int doctorId)
        {

            return !(!IsValidTime(startDate, takenExamination) && IsDoctorEqual(doctorId, takenExamination));
        }

        private List<Examination> GetTakenExaminations(int doctorId, string startTime, string endTime, DateTime examinationFinalDate)
        {
            List<Examination> examinations = new List<Examination>();
            if(Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

            DateTime start = Helpers.GetMergedDateTime(DateTime.Now, startTime);
            DateTime end = Helpers.GetMergedDateTime(examinationFinalDate, endTime);

            string query = "select * from examination where id_doctor = " + doctorId + " and dateOf between #" + start.ToString() + "# and #" + end.ToString() + "#";

            OleDbCommand cmd = DatabaseHelpers.GetCommand(query, Connection);
            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Examination examination = SetExaminationValues(reader);
                examinations.Add(examination);
            }
            if(Connection.State == System.Data.ConnectionState.Open) Connection.Close();

            return examinations;
        }

        private static Examination SetExaminationValues(OleDbDataReader reader)
        {
            TypeOfExamination typeOfExamination;
            Enum.TryParse<TypeOfExamination>(reader["typeOfExamination"].ToString(), out typeOfExamination);

            Examination examination = new Examination(
                Convert.ToInt32(reader["id_doctor"]),
                Convert.ToInt32(reader["id_patient"]),
                false,
                false,
                false,
                (DateTime)reader["dateOf"],
                typeOfExamination,
                false,
                Convert.ToInt32(reader["id_room"]),
                Convert.ToInt32(reader["duration"])
                );
            return examination;
        }
    }
}
