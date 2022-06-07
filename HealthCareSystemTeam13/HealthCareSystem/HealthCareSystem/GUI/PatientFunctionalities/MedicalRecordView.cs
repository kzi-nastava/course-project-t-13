﻿using HealthCareSystem.Core.Examinations.Controller;
using HealthCareSystem.Core.Examinations.Model;
using HealthCareSystem.Core.Examinations.Repository;
using HealthCareSystem.Core.Users.Patients.Repository;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HealthCareSystem.Core.GUI.PatientFunctionalities
{
    public partial class MedicalRecordView : Form
    {
        public string Username { get; set; }
        private PatientRepository _patientRepository;
        private ExaminationRepository _examinationRepository;
        private int PatientId;
        private List<DoctorAnamnesis> anamnesises;
        private AnamnesisRepository _anamnsesisRepository;
        
        public MedicalRecordView(string username)
        {
            Username = username;
            _patientRepository = new PatientRepository(Username);
            _examinationRepository = new ExaminationRepository();
            _anamnsesisRepository = new AnamnesisRepository();
            InitializeComponent();
        }

        private void MedicalRecordView_Load(object sender, EventArgs e)
        {
            SetValues();
        }
        private void SetValues()
        {
            PatientId = _patientRepository.GetPatientId();
            
            SetTextValues();
            SetDgwExaminations();
            SetListBoxDiseases();
            SetDgwAnamnesis();
        }
        private void SetTextValues()
        {
            Dictionary<string, string> information = _patientRepository.GetPatientNameAndMedicalStats(PatientId);
            lbHeight.Text = information["height"] + " cm";
            lbWeight.Text = information["weight"] + " kg";
            lbName.Text = information["firstName"] + " " + information["lastName"];
        }
        private void SetDgwExaminations()
        {
            _examinationRepository.PullFinishedExaminations(_patientRepository.GetPatientId());
            dgwExaminations.DataSource = _patientRepository.Examinations;
            GUIHelpers.DataGridViewSettings(dgwExaminations);
            dgwExaminations.Font = new Font("Lucida Bright", 10);

        }
        private void SetDgwAnamnesis()
        {
            List<Examination> examinations = _examinationRepository.GetFinishedExaminations(PatientId);

            anamnesises = _anamnsesisRepository.GetAnamnesises(examinations);

            dgwAnamnesis.DataSource = anamnesises;

            GUIHelpers.DataGridViewSettings(dgwAnamnesis);
            dgwAnamnesis.Font = new Font("Lucida Bright", 10);
        }

        private void SetListBoxDiseases()
        {
            
            int medicalRecordId = Convert.ToInt32(DatabaseCommander.ExecuteReaderQueries("select id from MedicalRecord where id_patient = " + PatientId + "", _patientRepository.Connection)[0]);
            List<string> diseases = DatabaseCommander.ExecuteReaderQueries("select nameOfDisease from DiseaseHistory where id_medicalRecord = " + medicalRecordId + "", _patientRepository.Connection);
            lbDiseases.DataSource = diseases;

        }

        private void btnShowAnamnesis_Click(object sender, EventArgs e)
        {
            if (GUIHelpers.IsDgwRowSelected(dgwExaminations))
            {
                int examinationId = (int)dgwExaminations.SelectedRows[0].Cells[0].Value;
                AnamnesisView anamnesisView = new AnamnesisView(examinationId);
                anamnesisView.ShowDialog();
            }
        }

        private void btnShowAnamnesisInSearch_Click(object sender, EventArgs e)
        {
            if (GUIHelpers.IsDgwRowSelected(dgwAnamnesis))
            {
                int examinationId = (int)dgwAnamnesis.SelectedRows[0].Cells[0].Value;
                AnamnesisView anamnesisView = new AnamnesisView(examinationId);
                anamnesisView.ShowDialog();
            }
        }

        private void btnSearchAnamnesis_Click(object sender, EventArgs e)
        {
            string keyword = tbAnamnesis.Text;
            if (keyword.Trim() != "") dgwAnamnesis.DataSource = _anamnsesisRepository.GetAnamnesisesByKeyword(anamnesises, keyword);
            else MessageBox.Show("No input.");
        }

        private void btnSortByDoctor_Click(object sender, EventArgs e)
        {
            anamnesises = ExaminationSorter.SortAnamnesises(anamnesises, 1);
            dgwAnamnesis.DataSource = anamnesises;
            dgwAnamnesis.Refresh();

        }

        private void btnSortBySpeciality_Click(object sender, EventArgs e)
        {
            anamnesises = ExaminationSorter.SortAnamnesises(anamnesises, 2);
            dgwAnamnesis.DataSource = anamnesises;
            dgwAnamnesis.Refresh();
        }

        private void btnSortByDate_Click(object sender, EventArgs e)
        {
            anamnesises = ExaminationSorter.SortAnamnesises(anamnesises);
            dgwAnamnesis.DataSource = anamnesises;
            dgwAnamnesis.Refresh();


        }

        private void tbAnamnesis_TextChanged(object sender, EventArgs e)
        {
            string keyword = tbAnamnesis.Text.Trim();
            if (keyword != "") dgwAnamnesis.DataSource = _anamnsesisRepository.GetAnamnesisesByKeyword(anamnesises, keyword);
            else dgwAnamnesis.DataSource = anamnesises;

        }
    }
}
