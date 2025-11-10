# API Versioning Implementation Guide

## 📊 Current Status

**❌ API Versioning is NOT currently implemented**

- All controllers use routes like: `api/[controller]`
- No version attributes found
- No versioning configuration in `Program.cs`
- No versioning NuGet packages installed

---

## 🎯 Why API Versioning?

API versioning allows you to:
- **Maintain backward compatibility** when making breaking changes
- **Support multiple API versions simultaneously** (v1, v2, etc.)
- **Gradually migrate clients** to new versions
- **Deprecate old versions** without breaking existing clients
- **Better API lifecycle management**

---

## 📈 Difficulty Assessment

### **Difficulty: ⭐⭐ Easy to Moderate** (2/5)

**Why it's relatively easy:**
- ASP.NET Core has built-in support via NuGet package
- Minimal code changes required
- Can be added incrementally
- Well-documented and widely used

**What makes it moderate:**
- Need to update all controllers
- Need to update all client services
- Need to decide on versioning strategy
- Need to handle version negotiation

**Estimated Time:** 2-4 hours for full implementation

---

## 🔧 Implementation Steps

### Step 1: Install NuGet Package

```bash
cd SM_MentalHealthApp.Server
dotnet add package Microsoft.AspNetCore.Mvc.Versioning
dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer
```

### Step 2: Configure Versioning in Program.cs

```csharp
// Add after builder.Services.AddControllers()
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Step 3: Update Swagger Configuration

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
    
    // Add versioning support to Swagger
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();
    
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new OpenApiInfo
        {
            Title = "Mental Health App API",
            Version = description.ApiVersion.ToString(),
            Description = description.IsDeprecated 
                ? "This API version has been deprecated." 
                : "Current API version"
        });
    }
    
    // Add version to Swagger UI
    options.DocInclusionPredicate((name, api) => true);
    options.EnableAnnotations();
});
```

### Step 4: Update Controllers

#### Option A: URL Segment Versioning (Recommended)

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AppointmentController : BaseController
{
    // Existing code...
}
```

**Result:** `api/v1/appointment`, `api/v2/appointment`

#### Option B: Query String Versioning

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : BaseController
{
    // Existing code...
}
```

**Result:** `api/appointment?api-version=1.0`

#### Option C: Header Versioning

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : BaseController
{
    // Existing code...
}
```

**Result:** Header: `X-Version: 1.0`

---

## 🎨 Recommended Approach: URL Segment Versioning

**Why URL Segment?**
- ✅ Most explicit and clear
- ✅ Easy to see version in URL
- ✅ RESTful and intuitive
- ✅ Works well with Swagger
- ✅ Easy to route and test

**Example URLs:**
- `GET /api/v1/appointment`
- `GET /api/v2/appointment`
- `POST /api/v1/clinicalnotes`

---

## 📝 Implementation Example

### Before (Current):

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments()
    {
        // ...
    }
}
```

### After (With Versioning):

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AppointmentController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments()
    {
        // Same code, no changes needed
    }
}
```

---

## 🔄 Client-Side Updates

### Update Service URLs

**Before:**
```csharp
var url = "api/appointment";
```

**After:**
```csharp
var url = "api/v1/appointment";
```

### Or Use Version Constant

```csharp
// SM_MentalHealthApp.Shared/Constants/ApiEndpoints.cs
public static class ApiEndpoints
{
    private const string ApiVersion = "v1";
    
    public static class Appointment
    {
        public const string Base = $"api/{ApiVersion}/appointment";
        public const string Validate = Base + "/validate";
        public const string Cancel = Base + "/{0}/cancel";
    }
}
```

---

## 🚀 Migration Strategy

### Phase 1: Add Versioning (Backward Compatible)

1. Add versioning package
2. Configure with `AssumeDefaultVersionWhenUnspecified = true`
3. Add `[ApiVersion("1.0")]` to all controllers
4. Keep routes as `api/[controller]` (defaults to v1)
5. **No breaking changes** - existing clients still work

### Phase 2: Update Routes (Optional)

1. Change routes to `api/v{version:apiVersion}/[controller]`
2. Update client services to use `v1` in URLs
3. Test thoroughly

### Phase 3: Add New Versions (When Needed)

```csharp
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AppointmentController : BaseController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointmentsV1()
    {
        // Old implementation
    }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetAppointmentsV2()
    {
        // New implementation with pagination
    }
}
```

---

## 📋 Implementation Checklist

### Server-Side
- [ ] Install `Microsoft.AspNetCore.Mvc.Versioning` package
- [ ] Install `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` package
- [ ] Configure versioning in `Program.cs`
- [ ] Update Swagger configuration
- [ ] Add `[ApiVersion("1.0")]` to all controllers
- [ ] Update routes to include version (optional)
- [ ] Test all endpoints

### Client-Side
- [ ] Update `ApiEndpoints` constants to include version
- [ ] Update all service implementations
- [ ] Test all API calls
- [ ] Update any hardcoded URLs

### Documentation
- [ ] Update API documentation
- [ ] Document versioning strategy
- [ ] Add deprecation notices (if any)

---

## 🎯 Quick Start (Minimal Changes)

### 1. Install Package

```bash
dotnet add SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj package Microsoft.AspNetCore.Mvc.Versioning
dotnet add SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer
```

### 2. Add to Program.cs

```csharp
// After builder.Services.AddControllers()
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
```

### 3. Add to Controllers

```csharp
[ApiVersion("1.0")]
[Route("api/[controller]")]  // Keep existing route
```

**That's it!** Existing clients continue to work, and you can now add v2 when needed.

---

## 🔍 Testing

### Test Version Negotiation

```bash
# Query string
curl https://api.example.com/api/appointment?api-version=1.0

# Header
curl -H "X-Version: 1.0" https://api.example.com/api/appointment

# URL segment (if using)
curl https://api.example.com/api/v1/appointment
```

---

## ⚠️ Important Considerations

### 1. **Backward Compatibility**
- Use `AssumeDefaultVersionWhenUnspecified = true` initially
- Allows existing clients to work without changes
- Gradually migrate to explicit versioning

### 2. **Version Strategy**
- **Semantic Versioning**: v1.0, v1.1, v2.0
- **Date-based**: v2024-01, v2024-02
- **Simple**: v1, v2, v3

### 3. **Deprecation**
- Mark old versions as deprecated
- Provide migration guides
- Set deprecation dates

### 4. **Breaking Changes**
- Major version bump (v1 → v2) for breaking changes
- Minor version bump (v1.0 → v1.1) for new features
- Patch version bump (v1.0.0 → v1.0.1) for bug fixes

---

## 📊 Impact Assessment

### Current Codebase
- **Controllers**: ~16 controllers need version attributes
- **Client Services**: ~10 services need URL updates
- **Breaking Changes**: None (if using `AssumeDefaultVersionWhenUnspecified`)

### Effort Required
- **Setup**: 30 minutes
- **Controller Updates**: 1-2 hours
- **Client Updates**: 1-2 hours
- **Testing**: 1 hour
- **Total**: 3-5 hours

---

## ✅ Benefits

1. **Future-proof**: Easy to add v2, v3, etc.
2. **Backward compatible**: Existing clients continue working
3. **Clear versioning**: Explicit version in URLs/headers
4. **Better Swagger**: Versioned API documentation
5. **Deprecation support**: Can mark versions as deprecated

---

## 🚦 Recommendation

**Start Simple:**
1. Add versioning package
2. Configure with default version
3. Add `[ApiVersion("1.0")]` to controllers
4. Keep existing routes (backward compatible)
5. Update client URLs when ready

**This approach:**
- ✅ No breaking changes
- ✅ Minimal code changes
- ✅ Easy to test
- ✅ Can migrate gradually

---

## 📚 Resources

- [Microsoft Docs: API Versioning](https://learn.microsoft.com/en-us/aspnet/core/web-api/versioning)
- [NuGet: Microsoft.AspNetCore.Mvc.Versioning](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Versioning)
- [API Versioning Best Practices](https://restfulapi.net/versioning/)

---

## 💡 Alternative: Manual Versioning

If you don't want to use the package, you can manually version:

```csharp
[Route("api/v1/[controller]")]
public class AppointmentV1Controller : BaseController { }

[Route("api/v2/[controller]")]
public class AppointmentV2Controller : BaseController { }
```

**Pros:** Simple, no dependencies
**Cons:** More code duplication, manual version management

---

## 🎯 Summary

**Current Status:** ❌ No versioning
**Difficulty:** ⭐⭐ Easy to Moderate (2/5)
**Time Required:** 3-5 hours
**Breaking Changes:** None (if done correctly)
**Recommendation:** ✅ Implement it - it's worth it for production!

The implementation is straightforward and can be done incrementally without breaking existing functionality.

