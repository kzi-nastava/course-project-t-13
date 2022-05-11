﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HealthCareSystem.Core.Users.Secretaries.Repository;


namespace HealthCareSystem.Core.GUI.SecretaryFunctionalities
{
    public partial class BlockPatient : Form
    {
        string Username;
        SecretaryRepository secretaryRepository;
        public BlockPatient(string username)
        {
            InitializeComponent();
            secretaryRepository = new SecretaryRepository();
            this.Username = username;
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            string patientId = patientIdBox.Text;
            secretaryRepository.BlockSinglePatient(patientId, Username);
            this.Close();
        }

    }
}