﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HealthCareSystem.Core.GUI.PatientFunctionalities;
using HealthCareSystem.Core.Users.Patients.Repository;

namespace HealthCareSystem.Core.GUI
{
    public partial class PatientView : Form
    {
        public string Username { get; set; }
        public LoginForm SuperForm;
        private PatientRepository _patientRepository;
        private System.Threading.Timer _timer;
        private int _notificationAlertTime;

        public PatientView(string username, LoginForm superForm)
        {
            Username = username;
            SuperForm = superForm;
            _patientRepository = new PatientRepository(username);
            _notificationAlertTime = _patientRepository.GetMedicationNotificationTime();

            InitializeComponent();

            LoadNotifications();
        }

        private void LoadForm(object Form)
        {
            if(this.pnlPatient.Controls.Count > 0)
            {
                this.pnlPatient.Controls.RemoveAt(0);

            }
            Form selectedButton = Form as Form;
            selectedButton.TopLevel = false;
            selectedButton.Dock = DockStyle.Fill;
            this.pnlPatient.Controls.Add(selectedButton);
            this.pnlPatient.Tag = selectedButton;
            selectedButton.Show();

        }
        private void PatientView_FormClosing(object sender, FormClosingEventArgs e)
        {
            SuperForm.Show();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult exit = MessageBox.Show("Are you sure?", "Logout?", MessageBoxButtons.YesNo);

            if (exit == DialogResult.Yes) { SuperForm.Show(); this.Close(); }
        }

        private void btnExaminations_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username))
            {
                LoadForm(new PatientExaminations(Username));
            }
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }

        private void PatientView_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            LoadForm(new HomeView(Username));
            Dictionary<int, DateTime> instructions =  _patientRepository.GetMedicationInstructions();
            
            
        }
        private void LoadNotifications()
        {
            Console.WriteLine("Da");
            Dictionary<int, DateTime> instructions = _patientRepository.GetMedicationInstructions();

            if (instructions.Count() == 0)
                return;

            int timesPerDay = instructions.Keys.First();
           
            List<int> atHours = new List<int>();
            int startHour = 8;
            int incremental = 23 / timesPerDay;

            while(startHour < 24 && timesPerDay > 0)
            {
                atHours.Add(startHour - _notificationAlertTime);
                startHour += incremental;
                timesPerDay -= 1;
            }

            DateTime startDate = instructions.Values.First();
            
            if(DateTime.Now >= startDate)
            {

                DateTime current = DateTime.Now;
                foreach (int atHour in atHours)
                {
                    int hour = atHour - current.Hour;

                    Console.WriteLine(hour);
                    Console.WriteLine(atHour);
                    Console.WriteLine(current.Hour);
                    if (hour > 0)
                    {
                        this._timer = new System.Threading.Timer(x =>
                        {
                            this.ShowNotification(atHour);
                        }, null, new TimeSpan(hour, 0, 0), Timeout.InfiniteTimeSpan);
                    }
                }

            }
            

        }
        private void ShowNotification(int atHour)
        {
            MessageBox.Show("Alerting you that you need to drink your medicine in " + atHour + " hours!");
        }
        private void btnAptRecc_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username)) LoadForm(new PatientRecommendation(Username));
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }

        private void btnMedicalRecord_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username)) LoadForm(new MedicalRecordView(Username));
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username)) LoadForm(new HomeView(Username));
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }
        

        private void btnHome_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnHome);
        }

        private void btnHome_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnHome);
        }

        private void btnExaminations_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnExaminations);

        }

        private void btnExaminations_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnExaminations);
        }

        private void btnAptRecc_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnAptRecc);
        }

        private void btnAptRecc_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnAptRecc);
        }

        private void btnMedicalRecord_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnMedicalRecord);
        }

        private void btnMedicalRecord_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnMedicalRecord);
        }

        private void btnExit_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnExit);
        }

        private void btnExit_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnExit);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            LoadForm(new HomeView(Username));
        }

        private void btnSearchDoctor_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username)) LoadForm(new SearchDoctorView(Username));
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }

        private void btnSearchDoctor_MouseEnter(object sender, EventArgs e)
        {
            Helpers.ButtonEnter(btnSearchDoctor);
        }

        private void btnSearchDoctor_MouseLeave(object sender, EventArgs e)
        {
            Helpers.ButtonLeave(btnSearchDoctor);
        }

        private void btnNotifications_Click(object sender, EventArgs e)
        {
            if (!_patientRepository.IsPatientBlocked(Username)) LoadForm(new NotificationsView(Username));
            else
            {
                MessageBox.Show("You are blocked!");
                SuperForm.Show(); this.Close();
            }
        }
    }
}
