﻿using HealthCareSystem.Core.Examinations.Model;
using HealthCareSystem.Core.Medications.Repository;
using HealthCareSystem.Core.Users.Doctors.Model;
using HealthCareSystem.Core.Users.Doctors.Repository;
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

namespace HealthCareSystem.Core.GUI.DoctorsFunctionalities
{
    public partial class StartedExamination : Form
    {
        private readonly PatientRepository _patientRep;
        private readonly MedicationRepository _medicationRep;
        private readonly DoctorRepository _doctorRep;
        private readonly MedicalRecordRepository _medicalRecordRep;
        private readonly ReferralLetterRepository _referralLetterRep;
        private readonly ReceiptRepository _receiptRep;
        private readonly InstructionRepository _instructionRep;
        private readonly int _patientId;
        private int _examinationId;
        private string _patientFullName;
        private Doctor _examiningDoctor;
        public StartedExamination(int examinationId, string patientFullName, string doctorUsername)
        {
            InitializeComponent();
            _patientRep = new PatientRepository();
            _doctorRep = new DoctorRepository(doctorUsername, true);
            _medicationRep = new MedicationRepository();
            _medicalRecordRep = new MedicalRecordRepository();
            _referralLetterRep = new ReferralLetterRepository();
            _receiptRep = new ReceiptRepository();
            _instructionRep = new InstructionRepository();


            _patientId = _patientRep.GetPatientIdByFirstName(patientFullName.Split(' ')[0]);
            _patientFullName = patientFullName;
            _examinationId = examinationId;
            _examiningDoctor = _doctorRep.GetDoctorByUsername();
            _medicationRep.PullMedicine();
            FillDataGridView();
        }

        private void FillDataGridView()
        {

            dgwMedications.DataSource = _medicationRep.Medicine;
            DataGridViewSettings();
        }

        private void DataGridViewSettings()
        {
            dgwMedications.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgwMedications.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgwMedications.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgwMedications.Columns[0].Width = 90;
            dgwMedications.Columns[1].Width = 90;
            dgwMedications.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private void ShowData()
        {
            string firstQuery = "select * from MedicalRecord where id_patient = " + _patientId;

            string[] firstData = _medicalRecordRep.GetMedicalRecord(firstQuery);
            lbPatientName.Text = "Examining: " + _patientFullName;
            lbHeight.Text = "Height: " + firstData[0];
            lbWeight.Text = "Weight: " + firstData[1];
        }

        private void FillComboBoxes()
        {
            cbSpeciality.DataSource = Enum.GetValues(typeof(DoctorSpeciality));
            cbSpeciality.ValueMember = null;
            cbSpeciality.DropDownStyle = ComboBoxStyle.DropDownList;


            BindingList<Doctor> doctors = _doctorRep.GetDoctors();
            var filteredBindingList = new BindingList<Doctor>
                (doctors.Where(doc => doc.FullName != _examiningDoctor.FullName).ToList());

            cbDoctor.ValueMember = null;
            cbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;

            cbDoctor.DisplayMember = "FullName";
            cbDoctor.DataSource = filteredBindingList;
        }

        private void StartedExamination_Load(object sender, EventArgs e)
        {

            ShowData();
            FillComboBoxes();

        }

        private void lbHeight_Click(object sender, EventArgs e)
        {

        }

        private void rbSpeciality_CheckedChanged(object sender, EventArgs e)
        {
            if(rbSpeciality.Checked)
            {
                cbSpeciality.Enabled = true;
                cbDoctor.Enabled = false;
            }
            else
            {
                cbDoctor.Enabled = true;
                cbSpeciality.Enabled = false;
            }
        }

        private void btnRefer_Click(object sender, EventArgs e)
        {
            ReferralLetter referralLetter;
            int option;
            if (rbDoctor.Checked)
            {
                Doctor forwardedDoctor = (Doctor)cbDoctor.SelectedValue;
                referralLetter = new ReferralLetter
                (_examiningDoctor.ID, _patientId,
                forwardedDoctor.ID, TypeOfExamination.BasicExamination);
                option = 1;
            }

            else
            {
                DoctorSpeciality doctorSpeciality = (DoctorSpeciality)cbSpeciality.SelectedValue;
                referralLetter = new ReferralLetter
                (_examiningDoctor.ID, _patientId,
                 TypeOfExamination.BasicExamination, doctorSpeciality);
                option = 2;
            }

            _referralLetterRep.InsertReferral(referralLetter, option);
            MessageBox.Show("Successfully created a referral for the patient");

        }

        private void cbDoctor_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbSpeciality_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void rbDoctor_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void lbWeight_Click(object sender, EventArgs e)
        {

        }

        private bool CanChangeExamination()
        {
            if (dgwMedications.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row first.");

            }
            else
            {
                DataGridViewRow row = dgwMedications.SelectedRows[0];
                if (row.Cells[0].Value == null)
                {
                    MessageBox.Show("You selected an empty row.");
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private void btnPrescribe_Click(object sender, EventArgs e)
        {
            if(CanChangeExamination())
            {
                List<int> medicationIds = new List<int>();
                var rows = dgwMedications.SelectedRows;
                for (int i = 0; i < dgwMedications.SelectedRows.Count; i++)
                {
                    medicationIds.Add((int)rows[i].Cells[0].Value);

                }

                if (!RejectIfPatientIsAlergic(medicationIds)) return;

                if (!CreateReceipt(medicationIds)) return;

                MessageBox.Show("Successfully created medical prescription for the patient!");
            }



        }

        private bool CreateReceipt(List<int> medicationIds)
        {
            if(rtbInstructions.Text == "")
            {
                MessageBox.Show("Instructions for the receipt can't be empty!");
                return false;
            }
            _instructionRep.InsertInstruction(rtbInstructions.Text);
            DateTime currentTime = DateTime.Now;

            _receiptRep.InsertReceipt(_examiningDoctor.ID, _patientId, currentTime);
            int lastReceiptId = _receiptRep.GetLastReceiptId();

            foreach (int medicationId in medicationIds)
            {
                _medicationRep.InsertConnectionOfReceiptAndMedication(lastReceiptId, medicationId);
            }
            return true;
        }

        private bool RejectIfPatientIsAlergic(List<int> medicationIds)
        {
            List<int> alergicMedicationIds = _medicationRep.GetAlergicMedicationsIds(_patientId);

            foreach (int medicationId in medicationIds)
            {
                foreach (int alergicMedicationId in alergicMedicationIds)
                {
                    if (medicationId == alergicMedicationId)
                    {
                        MessageBox.Show(_patientFullName + " is alergic to " +
                            _medicationRep.GetMedicationNameById(alergicMedicationId) +
                            "! Try again.");
                        return false;
                    }
                }
            }
            return true;
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            SetUsedDynamicEquipment setUsedDynamicEquipmentForm = new SetUsedDynamicEquipment(_examinationId, _doctorRep.Username);
            setUsedDynamicEquipmentForm.ShowDialog();
            this.Close();
        }
    }
}
