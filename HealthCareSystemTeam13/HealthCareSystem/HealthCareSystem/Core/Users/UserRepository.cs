﻿using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCareSystem.Core.Users
{
    class UserRepository
    {
        public OleDbConnection Connection { get; set; }

        public UserRepository()
        {
            try
            {
                Connection = new OleDbConnection();

                Connection.ConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=../../Data/HCDb.mdb;
                    Persist Security Info=False;";

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }
}