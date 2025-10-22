namespace CosmosDBIngester;

public class DisclaimerDialog : Form
{
    private CheckBox chkDoNotShowAgain = null!;
    private Button btnAccept = null!;
    private Button btnDecline = null!;
    
    public bool DoNotShowAgain => chkDoNotShowAgain.Checked;
    
    public DisclaimerDialog()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        this.Text = "⚠️ Important Security & Compliance Notice";
        this.Size = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        
        // Warning Icon
        var picWarning = new PictureBox
        {
            Location = new Point(20, 20),
            Size = new Size(48, 48),
            Image = SystemIcons.Warning.ToBitmap()
        };
        
        // Title
        var lblTitle = new Label
        {
            Text = "🛡️ SECURITY & COMPLIANCE DISCLAIMER",
            Location = new Point(80, 20),
            Size = new Size(580, 30),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.DarkRed
        };
        
        // Disclaimer Text
        var txtDisclaimer = new TextBox
        {
            Location = new Point(20, 70),
            Size = new Size(640, 350),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10),
            Text = @"📋 TEST DATA ONLY - READ CAREFULLY

This application generates MOCK/SYNTHETIC data for testing purposes ONLY.

⚠️ CRITICAL WARNINGS:

1. HEALTHCARE DATA (HIPAA):
   • This tool generates MOCK patient health information (PHI)
   • Data is randomly generated and NOT real patient data
   • DO NOT use this tool with production HIPAA-regulated systems
   • DO NOT connect this to databases containing real patient data
   • Using test data in HIPAA environments may violate compliance regulations
   • Fines for HIPAA violations can exceed $50,000 per incident

2. FINANCIAL DATA (PCI DSS, SOX):
   • Generated financial transactions are FAKE
   • DO NOT use with production banking or payment systems
   • DO NOT connect to systems handling real financial transactions
   • Test data in production financial systems violates compliance

3. E-COMMERCE DATA (GDPR, CCPA):
   • Customer names, emails, addresses are randomly generated
   • DO NOT use with production customer databases
   • Test data mixed with real PII violates privacy regulations

4. SECURITY CONSIDERATIONS:
   • Your Cosmos DB credentials are stored in memory during operation
   • Close the application when not in use
   • DO NOT share your Cosmos DB primary keys
   • Monitor your Azure costs - this tool can generate significant usage

5. COST WARNINGS:
   • This tool can consume large amounts of Azure Cosmos DB resources
   • High throughput settings can result in substantial Azure charges
   • Monitor your ingestion rates and stop if costs exceed expectations
   • You are responsible for all Azure costs incurred

6. INTENDED USE:
   ✅ Performance testing of Cosmos DB configurations
   ✅ Load testing with synthetic data
   ✅ Development and testing environments only
   ✅ Learning and training purposes

   ❌ Production environments with real data
   ❌ HIPAA, PCI DSS, SOX, or GDPR regulated systems
   ❌ Any system containing real personal information
   ❌ Unmonitored or uncontrolled cost environments

📜 BY USING THIS APPLICATION YOU ACKNOWLEDGE:
   • You understand this generates TEST DATA ONLY
   • You will NOT use this with production systems containing real data
   • You accept responsibility for compliance with all applicable regulations
   • You accept responsibility for all Azure costs incurred
   • You have appropriate authorization to access the Cosmos DB resources

⚖️ LEGAL NOTICE:
This application is provided ""AS IS"" without warranty of any kind. The authors
and distributors are not responsible for any compliance violations, data breaches,
or costs incurred through use of this application.

If you do not agree with these terms, click DECLINE and close the application."
        };
        
        // Do not show again checkbox
        chkDoNotShowAgain = new CheckBox
        {
            Text = "I understand and accept these terms (do not show again)",
            Location = new Point(20, 435),
            Size = new Size(640, 25),
            Font = new Font("Segoe UI", 10)
        };
        
        // Accept button
        btnAccept = new Button
        {
            Text = "✅ Accept and Continue",
            Location = new Point(440, 470),
            Size = new Size(200, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnAccept.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };
        
        // Decline button
        btnDecline = new Button
        {
            Text = "❌ Decline and Exit",
            Location = new Point(320, 470),
            Size = new Size(110, 35),
            BackColor = Color.FromArgb(232, 17, 35),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10)
        };
        btnDecline.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        
        this.Controls.AddRange(new Control[] { picWarning, lblTitle, txtDisclaimer, chkDoNotShowAgain, btnAccept, btnDecline });
    }
}
