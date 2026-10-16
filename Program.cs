using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartHealthcare.API.Services;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.AI.Tools;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// CORS Configuration
// =========================================================

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174"
    ];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://smart-healthcare-appointment-and-me.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =========================================================
// Database Configuration
// =========================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' was not found."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// =========================================================
// JWT Authentication Configuration
// =========================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT key 'Jwt:Key' was not found."
    );
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// =========================================================
// Application Service Registrations
// =========================================================

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SpecializationService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<DoctorScheduleService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<LabReportService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<InsuranceClaimService>();
builder.Services.AddScoped<InsurancePolicyService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReceptionistService>();
builder.Services.AddScoped<PatientService>();

// =========================================================
// AI Services
// =========================================================

// AI approval service
builder.Services.AddScoped<IAIApprovalService, AIApprovalService>();

// AI workflow and recommendation services
builder.Services.AddScoped<IAIWorkflowService, AIWorkflowService>();
builder.Services.AddScoped<IAIRecommendationService, AIRecommendationService>();

// AI orchestrator
builder.Services.AddScoped<IAIOrchestrator, AIOrchestrator>();

// Gemini LLM service
builder.Services.AddScoped<ILLMService, LLMService>();

// Appointment scheduling tool
builder.Services.AddScoped<IAppointmentSchedulingTool, AppointmentSchedulingTool>();

// Healthcare AI agents
builder.Services.AddScoped<IHealthcareAgent, AppointmentSchedulingAgent>();
builder.Services.AddScoped<IHealthcareAgent, PatientTriageAgent>();

// Medical summary agent
builder.Services.AddScoped<IHealthcareAgent, MedicalSummaryAgent>();

// Billing validation agent
builder.Services.AddScoped<IHealthcareAgent, BillingValidationAgent>();

// AI tools
builder.Services.AddScoped<IAppointmentSchedulingTool, AppointmentSchedulingTool>();
builder.Services.AddScoped<IMedicalSummaryTool, MedicalSummaryTool>();
builder.Services.AddScoped<IBillingValidationTool, BillingValidationTool>();

// =========================================================
// Controllers and Swagger
// =========================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// =========================================================
// Build Application
// =========================================================

var app = builder.Build();

// =========================================================
// HTTP Request Pipeline
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }