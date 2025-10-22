using CosmosDBIngester.Models;
using CosmosDBIngester.Services;
using CosmosDBIngester.Security;
using System.Windows.Forms;
using System.Configuration;

namespace CosmosDBIngester;

public partial class MainForm : Form
{
    private CosmosDbService _cosmosService;
    private CosmosConfig _config = new CosmosConfig();
    private CancellationTokenSource? _cancellationTokenSource;
    private System.Windows.Forms.Timer _statsTimer;
    private System.Windows.Forms.Timer _idleTimer;  // Security: Auto-clear credentials
    private bool _isDarkMode = true;
    private const int StatsTimerIntervalMs = 100;
    private const int IdleTimeoutMinutes = 30;  // Security: 30 minute idle timeout
    private DateTime _lastActivityTime;
    private readonly AuditLogger _auditLogger = new AuditLogger();

    private TextBox txtEndpoint = null!;
    private TextBox txtPrimaryKey = null!;
    private TextBox txtDatabase = null!;
    private TextBox txtCollection = null!;
    private NumericUpDown numThroughput = null!;
    private NumericUpDown numBatchSize = null!;
    private NumericUpDown numDocumentSize = null!;
    private ComboBox cmbDataType = null!;
    private ComboBox cmbWorkloadType = null!;
    private Button btnConnect = null!;
    private Button btnDisconnect = null!;
    private Button btnStart = null!;
    private Button btnStop = null!;
    private Button btnToggleTheme = null!;
    private TextBox txtStatus = null!;
    private Label lblDocuments = null!;
    private Label lblDataSize = null!;
    private Label lblDocsPerSec = null!;
    private Label lblMBPerSec = null!;
    private GroupBox grpConnection = null!;
    private GroupBox grpSettings = null!;
    private GroupBox grpStats = null!;

    public MainForm()
    {
        // Security: Show disclaimer on first run
        var disclaimerShown = Properties.Settings.Default.DisclaimerAccepted;
        if (!disclaimerShown)
        {
            var disclaimer = new DisclaimerDialog();
            if (disclaimer.ShowDialog() != DialogResult.OK)
            {
                // User declined - exit application
                Environment.Exit(0);
                return;
            }
            
            if (disclaimer.DoNotShowAgain)
            {
                Properties.Settings.Default.DisclaimerAccepted = true;
                Properties.Settings.Default.Save();
            }
        }
        
        _cosmosService = new CosmosDbService();
        _cosmosService.OnStatusChanged += OnStatusChanged;
        _cosmosService.OnStatsUpdated += OnStatsUpdated;
        
        _statsTimer = new System.Windows.Forms.Timer();
        _statsTimer.Interval = StatsTimerIntervalMs;
        _statsTimer.Tick += (s, e) => { }; // Removed Application.DoEvents() anti-pattern
        
        // Security: Idle timeout timer
        _idleTimer = new System.Windows.Forms.Timer();
        _idleTimer.Interval = 60000; // Check every minute
        _idleTimer.Tick += IdleTimer_Tick;
        _idleTimer.Start();
        _lastActivityTime = DateTime.UtcNow;
        
        InitializeComponent();
        
        _auditLogger.LogSecurityEvent("ApplicationStarted", $"Version: 1.0.0, User: {Environment.UserName}", "Info");
    }

    private void InitializeComponent()
    {
        this.Text = "Cosmos DB Bulk Data Ingester";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;

        // Theme Toggle Button
        btnToggleTheme = new Button
        {
            Text = "☀️ Light Mode",
            Location = new Point(10, 10),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9)
        };
        btnToggleTheme.FlatAppearance.BorderColor = Color.FromArgb(63, 63, 70);
        btnToggleTheme.Click += BtnToggleTheme_Click;

        // Connection Group
        grpConnection = new GroupBox
        {
            Text = "Connection Settings",
            Location = new Point(10, 50),
            Size = new Size(860, 180),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Label lblEndpoint = new Label { Text = "Cosmos DB Endpoint:", Location = new Point(10, 25), AutoSize = true };
        txtEndpoint = new TextBox { Location = new Point(150, 22), Size = new Size(680, 25), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        Label lblKey = new Label { Text = "Primary Key:", Location = new Point(10, 55), AutoSize = true };
        txtPrimaryKey = new TextBox { Location = new Point(150, 52), Size = new Size(680, 25), UseSystemPasswordChar = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        Label lblDb = new Label { Text = "Database Name:", Location = new Point(10, 85), AutoSize = true };
        txtDatabase = new TextBox { Location = new Point(150, 82), Size = new Size(300, 25) };

        Label lblColl = new Label { Text = "Collection Name:", Location = new Point(10, 115), AutoSize = true };
        txtCollection = new TextBox { Location = new Point(150, 112), Size = new Size(300, 25) };

        Label lblThroughput = new Label { Text = "Throughput (RUs):", Location = new Point(470, 85), AutoSize = true };
        numThroughput = new NumericUpDown { Location = new Point(590, 82), Size = new Size(100, 25), Minimum = 400, Maximum = 1000000, Value = 400 };

        btnConnect = new Button { Text = "Connect", Location = new Point(620, 145), Size = new Size(100, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnConnect.Click += BtnConnect_Click;

        btnDisconnect = new Button 
        { 
            Text = "Change Connection", 
            Location = new Point(730, 145), 
            Size = new Size(140, 30),
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnDisconnect.Click += BtnDisconnect_Click;

        grpConnection.Controls.AddRange(new Control[] { lblEndpoint, txtEndpoint, lblKey, txtPrimaryKey, lblDb, txtDatabase, lblColl, txtCollection, lblThroughput, numThroughput, btnConnect, btnDisconnect });

        // Settings Group
        grpSettings = new GroupBox
        {
            Text = "Ingestion Settings",
            Location = new Point(10, 240),
            Size = new Size(860, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Create tooltip for descriptions
        var tooltip = new ToolTip();
        tooltip.AutoPopDelay = 10000;
        tooltip.InitialDelay = 500;

        Label lblDataType = new Label { Text = "Data Type:", Location = new Point(10, 25), AutoSize = true };
        cmbDataType = new ComboBox { Location = new Point(150, 22), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDataType.Items.AddRange(new object[] { "Financial", "E-Commerce", "Healthcare", "IoT" });
        cmbDataType.SelectedIndex = 0;
        tooltip.SetToolTip(cmbDataType, 
            "Financial: Banking transactions, payments, and account data\n" +
            "E-Commerce: Orders, products, customers, and shipping info\n" +
            "Healthcare: Patient records, diagnoses, treatments, and vitals\n" +
            "IoT: Device telemetry, sensors, location, and readings");

        Label lblWorkload = new Label { Text = "Workload Type:", Location = new Point(10, 55), AutoSize = true };
        cmbWorkloadType = new ComboBox { Location = new Point(150, 52), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbWorkloadType.Items.AddRange(new object[] { "Sequential", "Random", "HotPartition" });
        cmbWorkloadType.SelectedIndex = 0;
        tooltip.SetToolTip(cmbWorkloadType,
            "Sequential: Even distribution across partitions (balanced load)\n" +
            "Random: Randomly assigned partitions (simulates varied access)\n" +
            "HotPartition: All data to one partition (tests partition saturation)");

        Label lblBatch = new Label { Text = "Batch Size:", Location = new Point(380, 25), AutoSize = true };
        numBatchSize = new NumericUpDown { Location = new Point(480, 22), Size = new Size(100, 25), Minimum = 1, Maximum = 1000, Value = 10 };
        tooltip.SetToolTip(numBatchSize, "Number of documents to create and insert per batch operation");

        Label lblDocSize = new Label { Text = "Document Size (KB):", Location = new Point(380, 55), AutoSize = true };
        numDocumentSize = new NumericUpDown { Location = new Point(510, 52), Size = new Size(70, 25), Minimum = 1, Maximum = 2048, Value = 1 };
        tooltip.SetToolTip(numDocumentSize, "Size of each document including padding data (affects throughput)");

        // Action Buttons (inside groupbox but positioned at bottom)
        btnStart = new Button 
        { 
            Text = "Start Ingestion", 
            Location = new Point(640, 85), 
            Size = new Size(110, 28), 
            Enabled = false,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnStart.Click += BtnStart_Click;

        btnStop = new Button 
        { 
            Text = "Stop Ingestion", 
            Location = new Point(755, 85), 
            Size = new Size(100, 28), 
            Enabled = false,
            BackColor = Color.FromArgb(232, 17, 35),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnStop.Click += BtnStop_Click;

        grpSettings.Controls.AddRange(new Control[] { lblDataType, cmbDataType, lblWorkload, cmbWorkloadType, lblBatch, numBatchSize, lblDocSize, numDocumentSize, btnStart, btnStop });

        // Stats Group
        grpStats = new GroupBox
        {
            Text = "Real-time Statistics",
            Location = new Point(10, 370),
            Size = new Size(860, 100),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        lblDocuments = new Label { Text = "Total Documents: 0", Location = new Point(20, 25), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        lblDataSize = new Label { Text = "Total Data Size: 0 MB", Location = new Point(20, 50), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        lblDocsPerSec = new Label { Text = "Speed: 0 docs/sec", Location = new Point(450, 25), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        lblMBPerSec = new Label { Text = "Throughput: 0 MB/sec", Location = new Point(450, 50), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

        grpStats.Controls.AddRange(new Control[] { lblDocuments, lblDataSize, lblDocsPerSec, lblMBPerSec });

        // Status TextBox
        Label lblStatus = new Label { Text = "Status Log:", Location = new Point(10, 480), AutoSize = true };
        txtStatus = new TextBox
        {
            Location = new Point(10, 505),
            Size = new Size(860, 180),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.Black,
            ForeColor = Color.Lime,
            Font = new Font("Consolas", 9),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        this.Controls.AddRange(new Control[] { btnToggleTheme, grpConnection, grpSettings, grpStats, lblStatus, txtStatus });

        // Apply initial dark mode theme
        ApplyTheme();
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        try
        {
            // Security: Update activity time
            _lastActivityTime = DateTime.UtcNow;
            
            if (string.IsNullOrWhiteSpace(txtEndpoint.Text) || 
                string.IsNullOrWhiteSpace(txtPrimaryKey.Text) ||
                string.IsNullOrWhiteSpace(txtDatabase.Text) ||
                string.IsNullOrWhiteSpace(txtCollection.Text))
            {
                MessageBox.Show("Please fill in all connection fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Security: Comprehensive input validation
            var endpointResult = InputValidator.ValidateEndpoint(txtEndpoint.Text.Trim());
            if (!endpointResult.IsValid)
            {
                MessageBox.Show(endpointResult.ErrorMessage, "Invalid Endpoint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var keyResult = InputValidator.ValidatePrimaryKey(txtPrimaryKey.Text.Trim());
            if (!keyResult.IsValid)
            {
                MessageBox.Show(keyResult.ErrorMessage, "Invalid Primary Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var dbResult = InputValidator.ValidateDatabaseName(txtDatabase.Text.Trim());
            if (!dbResult.IsValid)
            {
                MessageBox.Show(dbResult.ErrorMessage, "Invalid Database Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var collResult = InputValidator.ValidateCollectionName(txtCollection.Text.Trim());
            if (!collResult.IsValid)
            {
                MessageBox.Show(collResult.ErrorMessage, "Invalid Collection Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var throughputResult = InputValidator.ValidateThroughput((int)numThroughput.Value);
            if (!throughputResult.IsValid)
            {
                MessageBox.Show(throughputResult.ErrorMessage, "Invalid Throughput", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _config.Endpoint = txtEndpoint.Text.Trim();
            _config.SetPrimaryKey(txtPrimaryKey.Text.Trim());  // Security: Use SecureString
            _config.DatabaseName = txtDatabase.Text.Trim();
            _config.CollectionName = txtCollection.Text.Trim();
            _config.ThroughputRUs = (int)numThroughput.Value;

            btnConnect.Enabled = false;
            btnConnect.Text = "Connecting...";

            var success = await _cosmosService.InitializeAsync(_config);

            if (success)
            {
                btnStart.Enabled = true;
                btnConnect.Enabled = false;
                
                // Disable individual connection input controls
                txtEndpoint.Enabled = false;
                txtPrimaryKey.Enabled = false;
                txtDatabase.Enabled = false;
                txtCollection.Enabled = false;
                numThroughput.Enabled = false;
                
                // Keep disconnect button enabled
                btnDisconnect.Enabled = true;
                
                MessageBox.Show("Successfully connected to Cosmos DB!\n\n⚠️ REMINDER: This tool generates TEST DATA ONLY.\nDo not use with production systems containing real data.", 
                    "Connection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                btnConnect.Enabled = true;
                btnConnect.Text = "Connect";
                MessageBox.Show("Failed to connect. Please check the status log for details.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            btnConnect.Enabled = true;
            btnConnect.Text = "Connect";
            _auditLogger.LogException("BtnConnect_Click", ex);
            var userMessage = SecureErrorHandler.GetUserFriendlyMessage(ex);
            MessageBox.Show(userMessage, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            OnStatusChanged($"Connection failed. Check logs for details.");
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        // Check if ingestion is running
        if (_cosmosService.IsRunning)
        {
            var result = MessageBox.Show(
                "Ingestion is currently running. Do you want to stop it and change the connection?",
                "Confirm Disconnect",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            
            if (result == DialogResult.No)
                return;
            
            // Stop ingestion
            _cosmosService.StopIngestion();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _statsTimer.Stop();
            btnStart.Enabled = false;
            btnStop.Enabled = false;
        }

        // Dispose current connection
        _cosmosService.Dispose();
        
        // Re-create the service
        _cosmosService = new CosmosDbService();
        _cosmosService.OnStatusChanged += OnStatusChanged;
        _cosmosService.OnStatsUpdated += OnStatsUpdated;

        // Re-enable connection settings individually
        txtEndpoint.Enabled = true;
        txtPrimaryKey.Enabled = true;
        txtDatabase.Enabled = true;
        txtCollection.Enabled = true;
        numThroughput.Enabled = true;
        btnConnect.Enabled = true;
        btnConnect.Text = "Connect";
        btnDisconnect.Enabled = false;
        btnStart.Enabled = false;
        
        // Reset stats
        lblDocuments.Text = "Total Documents: 0";
        lblDataSize.Text = "Total Data Size: 0 MB";
        lblDocsPerSec.Text = "Speed: 0 docs/sec";
        lblMBPerSec.Text = "Throughput: 0 MB/sec";
        
        OnStatusChanged("Disconnected. Ready for new connection.");
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        OnStatusChanged("Start button clicked!");
        
        _config.DataType = cmbDataType.SelectedIndex switch
        {
            0 => DataType.Financial,
            1 => DataType.ECommerce,
            2 => DataType.Healthcare,
            3 => DataType.IoT,
            _ => DataType.Financial
        };
        _config.WorkloadType = cmbWorkloadType.Text;
        _config.BatchSize = (int)numBatchSize.Value;
        _config.DocumentSizeKB = (int)numDocumentSize.Value;

        // Dispose previous cancellation token if exists
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        // Disable input controls only
        cmbDataType.Enabled = false;
        cmbWorkloadType.Enabled = false;
        numBatchSize.Enabled = false;
        numDocumentSize.Enabled = false;
        btnStart.Enabled = false;
        
        // Enable Stop button
        btnStop.Enabled = true;
        
        _statsTimer.Start();

        OnStatusChanged($"Starting ingestion with DataType: {_config.DataType}");

        // Run ingestion on background thread
        _ = Task.Run(async () =>
        {
            try
            {
                await _cosmosService.StartIngestionAsync(_config, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                this.Invoke(() => OnStatusChanged("Ingestion cancelled."));
            }
            catch (Exception ex)
            {
                this.Invoke(() =>
                {
                    OnStatusChanged($"Error: {ex.GetType().Name} - {ex.Message}");
                    MessageBox.Show($"Ingestion error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            finally
            {
                // Re-enable controls when ingestion completes
                this.Invoke(() =>
                {
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    cmbDataType.Enabled = true;
                    cmbWorkloadType.Enabled = true;
                    numBatchSize.Enabled = true;
                    numDocumentSize.Enabled = true;
                    _statsTimer.Stop();
                });
            }
        });
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        OnStatusChanged("Stop button clicked - stopping ingestion...");
        
        _cosmosService.StopIngestion();
        _cancellationTokenSource?.Cancel();
        _statsTimer.Stop();

        btnStart.Enabled = true;
        btnStop.Enabled = false;
        cmbDataType.Enabled = true;
        cmbWorkloadType.Enabled = true;
        numBatchSize.Enabled = true;
        numDocumentSize.Enabled = true;
    }

    private void OnStatusChanged(string status)
    {
        if (txtStatus.InvokeRequired)
        {
            txtStatus.Invoke(() => OnStatusChanged(status));
            return;
        }

        txtStatus.AppendText($"[{DateTime.Now:HH:mm:ss}] {status}{Environment.NewLine}");
        txtStatus.SelectionStart = txtStatus.Text.Length;
        txtStatus.ScrollToCaret();
    }

    private void OnStatsUpdated(IngestionStats stats)
    {
        if (lblDocuments.InvokeRequired)
        {
            lblDocuments.Invoke(() => OnStatsUpdated(stats));
            return;
        }

        lblDocuments.Text = $"Total Documents: {stats.TotalDocuments:N0}";
        lblDataSize.Text = $"Total Data Size: {stats.TotalDataSizeKB / 1024:N2} MB";
        lblDocsPerSec.Text = $"Speed: {stats.DocumentsPerSecond:N1} docs/sec";
        lblMBPerSec.Text = $"Throughput: {stats.KBPerSecond / 1024:N2} MB/sec";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cosmosService.StopIngestion();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _statsTimer?.Stop();
        _statsTimer?.Dispose();
        _idleTimer?.Stop();
        _idleTimer?.Dispose();
        _cosmosService.Dispose();
        
        // Security: Clear credentials from memory
        _config.ClearCredentials();
        txtPrimaryKey.Text = string.Empty;
        
        // Security: Audit log application close
        _auditLogger.LogSecurityEvent("ApplicationClosed", $"User: {Environment.UserName}", "Info");
        _auditLogger.Dispose();
        
        base.OnFormClosing(e);
    }
    
    // Security: Idle timeout handler
    private void IdleTimer_Tick(object? sender, EventArgs e)
    {
        var idleTime = DateTime.UtcNow - _lastActivityTime;
        if (idleTime.TotalMinutes >= IdleTimeoutMinutes)
        {
            // Stop any running ingestion
            if (_cosmosService.IsRunning)
            {
                _cosmosService.StopIngestion();
                _cancellationTokenSource?.Cancel();
                _statsTimer.Stop();
            }
            
            // Clear credentials
            _config.ClearCredentials();
            txtPrimaryKey.Text = string.Empty;
            
            // Reset connection
            BtnDisconnect_Click(null, EventArgs.Empty);
            
            _auditLogger.LogSecurityEvent("IdleTimeout", $"Session timed out after {IdleTimeoutMinutes} minutes of inactivity", "Warning");
            
            MessageBox.Show(
                $"Your session has timed out due to {IdleTimeoutMinutes} minutes of inactivity.\n\nFor security reasons, your credentials have been cleared.\nPlease reconnect to continue.",
                "Session Timeout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            
            _lastActivityTime = DateTime.UtcNow; // Reset to prevent repeated alerts
        }
    }
    
    // Security: Update activity time on mouse/keyboard interaction
    protected override void OnMouseMove(MouseEventArgs e)
    {
        _lastActivityTime = DateTime.UtcNow;
        base.OnMouseMove(e);
    }
    
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        _lastActivityTime = DateTime.UtcNow;
        base.OnKeyPress(e);
    }

    private void BtnToggleTheme_Click(object? sender, EventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_isDarkMode)
        {
            // Dark Mode
            this.BackColor = Color.FromArgb(30, 30, 30);
            btnToggleTheme.Text = "☀️ Light Mode";
            btnToggleTheme.BackColor = Color.FromArgb(45, 45, 48);
            btnToggleTheme.ForeColor = Color.White;

            // Group boxes
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is GroupBox groupBox)
                {
                    groupBox.ForeColor = Color.White;
                    ApplyThemeToGroupBox(groupBox, true);
                }
            }

            // Status log already has dark colors
            txtStatus.BackColor = Color.Black;
            txtStatus.ForeColor = Color.Lime;
        }
        else
        {
            // Light Mode
            this.BackColor = Color.FromArgb(240, 240, 240);
            btnToggleTheme.Text = "🌙 Dark Mode";
            btnToggleTheme.BackColor = Color.FromArgb(225, 225, 225);
            btnToggleTheme.ForeColor = Color.Black;

            // Group boxes
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is GroupBox groupBox)
                {
                    groupBox.ForeColor = Color.Black;
                    ApplyThemeToGroupBox(groupBox, false);
                }
            }

            // Status log in light mode
            txtStatus.BackColor = Color.White;
            txtStatus.ForeColor = Color.Black;
        }
    }

    private void ApplyThemeToGroupBox(GroupBox groupBox, bool isDark)
    {
        foreach (Control ctrl in groupBox.Controls)
        {
            if (ctrl is Label label)
            {
                label.ForeColor = isDark ? Color.White : Color.Black;
            }
            else if (ctrl is TextBox textBox && textBox != txtStatus)
            {
                textBox.BackColor = isDark ? Color.FromArgb(45, 45, 48) : Color.White;
                textBox.ForeColor = isDark ? Color.White : Color.Black;
            }
            else if (ctrl is ComboBox comboBox)
            {
                comboBox.BackColor = isDark ? Color.FromArgb(45, 45, 48) : Color.White;
                comboBox.ForeColor = isDark ? Color.White : Color.Black;
            }
            else if (ctrl is NumericUpDown numericUpDown)
            {
                numericUpDown.BackColor = isDark ? Color.FromArgb(45, 45, 48) : Color.White;
                numericUpDown.ForeColor = isDark ? Color.White : Color.Black;
            }
            else if (ctrl is Button button && button != btnStart && button != btnStop)
            {
                if (button == btnConnect || button == btnDisconnect)
                {
                    button.BackColor = isDark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(225, 225, 225);
                    button.ForeColor = isDark ? Color.White : Color.Black;
                }
            }
        }
    }
}
