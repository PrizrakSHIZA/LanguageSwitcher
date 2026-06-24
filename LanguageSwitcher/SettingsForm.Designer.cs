namespace LanguageSwitcher
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lbActiveSwitch = new System.Windows.Forms.ListBox();
            this.lbDisabledSwitch = new System.Windows.Forms.ListBox();
            this.btnDisableLanguage = new System.Windows.Forms.Button();
            this.btnEnableLanguage = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblActive = new System.Windows.Forms.Label();
            this.lblDisabled = new System.Windows.Forms.Label();
            this.lblCycleHotkey = new System.Windows.Forms.Label();
            this.lblFallbackHotkey = new System.Windows.Forms.Label();
            this.lblLanguageHotkey = new System.Windows.Forms.Label();
            this.cycleHotkeyBox = new LanguageSwitcher.HotkeyTextBox();
            this.fallbackHotkeyBox = new LanguageSwitcher.HotkeyTextBox();
            this.languageHotkeyBox = new LanguageSwitcher.HotkeyTextBox();
            this.btnClearLanguageHotkey = new System.Windows.Forms.Button();
            this.chkRunAtStartup = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lbActiveSwitch
            // 
            this.lbActiveSwitch.DisplayMember = "DisplayName";
            this.lbActiveSwitch.FormattingEnabled = true;
            this.lbActiveSwitch.Location = new System.Drawing.Point(12, 32);
            this.lbActiveSwitch.Name = "lbActiveSwitch";
            this.lbActiveSwitch.Size = new System.Drawing.Size(290, 212);
            this.lbActiveSwitch.TabIndex = 1;
            // 
            // lbDisabledSwitch
            // 
            this.lbDisabledSwitch.DisplayMember = "DisplayName";
            this.lbDisabledSwitch.FormattingEnabled = true;
            this.lbDisabledSwitch.Location = new System.Drawing.Point(393, 32);
            this.lbDisabledSwitch.Name = "lbDisabledSwitch";
            this.lbDisabledSwitch.Size = new System.Drawing.Size(290, 212);
            this.lbDisabledSwitch.TabIndex = 5;
            // 
            // btnDisableLanguage
            // 
            this.btnDisableLanguage.Location = new System.Drawing.Point(321, 88);
            this.btnDisableLanguage.Name = "btnDisableLanguage";
            this.btnDisableLanguage.Size = new System.Drawing.Size(55, 27);
            this.btnDisableLanguage.TabIndex = 3;
            this.btnDisableLanguage.Text = ">";
            this.btnDisableLanguage.UseVisualStyleBackColor = true;
            // 
            // btnEnableLanguage
            // 
            this.btnEnableLanguage.Location = new System.Drawing.Point(321, 128);
            this.btnEnableLanguage.Name = "btnEnableLanguage";
            this.btnEnableLanguage.Size = new System.Drawing.Size(55, 27);
            this.btnEnableLanguage.TabIndex = 4;
            this.btnEnableLanguage.Text = "<";
            this.btnEnableLanguage.UseVisualStyleBackColor = true;
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Location = new System.Drawing.Point(12, 253);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(75, 27);
            this.btnMoveUp.TabIndex = 6;
            this.btnMoveUp.Text = "Up";
            this.btnMoveUp.UseVisualStyleBackColor = true;
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Location = new System.Drawing.Point(93, 253);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(75, 27);
            this.btnMoveDown.TabIndex = 7;
            this.btnMoveDown.Text = "Down";
            this.btnMoveDown.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(527, 390);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 27);
            this.btnSave.TabIndex = 14;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(608, 390);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 27);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblActive
            // 
            this.lblActive.AutoSize = true;
            this.lblActive.Location = new System.Drawing.Point(12, 13);
            this.lblActive.Name = "lblActive";
            this.lblActive.Size = new System.Drawing.Size(141, 13);
            this.lblActive.TabIndex = 0;
            this.lblActive.Text = "Enabled cycle languages";
            // 
            // lblDisabled
            // 
            this.lblDisabled.AutoSize = true;
            this.lblDisabled.Location = new System.Drawing.Point(390, 13);
            this.lblDisabled.Name = "lblDisabled";
            this.lblDisabled.Size = new System.Drawing.Size(151, 13);
            this.lblDisabled.TabIndex = 2;
            this.lblDisabled.Text = "Disabled cycle languages";
            // 
            // lblCycleHotkey
            // 
            this.lblCycleHotkey.AutoSize = true;
            this.lblCycleHotkey.Location = new System.Drawing.Point(12, 306);
            this.lblCycleHotkey.Name = "lblCycleHotkey";
            this.lblCycleHotkey.Size = new System.Drawing.Size(69, 13);
            this.lblCycleHotkey.TabIndex = 8;
            this.lblCycleHotkey.Text = "Cycle hotkey";
            // 
            // lblLanguageHotkey
            // 
            this.lblLanguageHotkey.AutoSize = true;
            this.lblLanguageHotkey.Location = new System.Drawing.Point(470, 306);
            this.lblLanguageHotkey.Name = "lblLanguageHotkey";
            this.lblLanguageHotkey.Size = new System.Drawing.Size(111, 13);
            this.lblLanguageHotkey.TabIndex = 12;
            this.lblLanguageHotkey.Text = "Selected language";
            // 
            // lblFallbackHotkey
            // 
            this.lblFallbackHotkey.AutoSize = true;
            this.lblFallbackHotkey.Location = new System.Drawing.Point(241, 306);
            this.lblFallbackHotkey.Name = "lblFallbackHotkey";
            this.lblFallbackHotkey.Size = new System.Drawing.Size(124, 13);
            this.lblFallbackHotkey.TabIndex = 10;
            this.lblFallbackHotkey.Text = "Windows fallback hotkey";
            // 
            // cycleHotkeyBox
            // 
            this.cycleHotkeyBox.Location = new System.Drawing.Point(15, 325);
            this.cycleHotkeyBox.Name = "cycleHotkeyBox";
            this.cycleHotkeyBox.Size = new System.Drawing.Size(190, 20);
            this.cycleHotkeyBox.TabIndex = 9;
            // 
            // fallbackHotkeyBox
            // 
            this.fallbackHotkeyBox.Location = new System.Drawing.Point(244, 325);
            this.fallbackHotkeyBox.Name = "fallbackHotkeyBox";
            this.fallbackHotkeyBox.Size = new System.Drawing.Size(190, 20);
            this.fallbackHotkeyBox.TabIndex = 11;
            // 
            // languageHotkeyBox
            // 
            this.languageHotkeyBox.Location = new System.Drawing.Point(473, 325);
            this.languageHotkeyBox.Name = "languageHotkeyBox";
            this.languageHotkeyBox.Size = new System.Drawing.Size(135, 20);
            this.languageHotkeyBox.TabIndex = 13;
            // 
            // btnClearLanguageHotkey
            // 
            this.btnClearLanguageHotkey.Location = new System.Drawing.Point(614, 322);
            this.btnClearLanguageHotkey.Name = "btnClearLanguageHotkey";
            this.btnClearLanguageHotkey.Size = new System.Drawing.Size(69, 27);
            this.btnClearLanguageHotkey.TabIndex = 14;
            this.btnClearLanguageHotkey.Text = "Clear";
            this.btnClearLanguageHotkey.UseVisualStyleBackColor = true;
            // 
            // chkRunAtStartup
            // 
            this.chkRunAtStartup.AutoSize = true;
            this.chkRunAtStartup.Location = new System.Drawing.Point(15, 365);
            this.chkRunAtStartup.Name = "chkRunAtStartup";
            this.chkRunAtStartup.Size = new System.Drawing.Size(94, 17);
            this.chkRunAtStartup.TabIndex = 15;
            this.chkRunAtStartup.Text = "Run at startup";
            this.chkRunAtStartup.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(695, 429);
            this.Controls.Add(this.chkRunAtStartup);
            this.Controls.Add(this.btnClearLanguageHotkey);
            this.Controls.Add(this.languageHotkeyBox);
            this.Controls.Add(this.fallbackHotkeyBox);
            this.Controls.Add(this.cycleHotkeyBox);
            this.Controls.Add(this.lblLanguageHotkey);
            this.Controls.Add(this.lblFallbackHotkey);
            this.Controls.Add(this.lblCycleHotkey);
            this.Controls.Add(this.lblDisabled);
            this.Controls.Add(this.lblActive);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.btnEnableLanguage);
            this.Controls.Add(this.btnDisableLanguage);
            this.Controls.Add(this.lbDisabledSwitch);
            this.Controls.Add(this.lbActiveSwitch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Language Switcher Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ListBox lbActiveSwitch;
        private System.Windows.Forms.ListBox lbDisabledSwitch;
        private System.Windows.Forms.Button btnDisableLanguage;
        private System.Windows.Forms.Button btnEnableLanguage;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblActive;
        private System.Windows.Forms.Label lblDisabled;
        private System.Windows.Forms.Label lblCycleHotkey;
        private System.Windows.Forms.Label lblFallbackHotkey;
        private System.Windows.Forms.Label lblLanguageHotkey;
        private HotkeyTextBox cycleHotkeyBox;
        private HotkeyTextBox fallbackHotkeyBox;
        private HotkeyTextBox languageHotkeyBox;
        private System.Windows.Forms.Button btnClearLanguageHotkey;
        private System.Windows.Forms.CheckBox chkRunAtStartup;
    }
}
