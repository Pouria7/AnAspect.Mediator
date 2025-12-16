# ExcludeTypedBehavior Feature

## 📋 Overview

Added new `ExcludeTypedBehavior<TBehavior, TRequest, TResponse>()` method to exclude specific typed behaviors by their concrete type, complementing the existing `ExcludeBehavior<TBehavior>()` which only works with marker interfaces.

## 🎯 Problem Solved

**Before:** You could only exclude behaviors that implement marker interfaces extending `IPipelineBehavior`:
```csharp
// This worked ✅
mediator.ExcludeBehavior<ILoggingBehavior>()

// This didn't work ❌ - CreateUserValidation implements IPipelineBehavior<CreateUserCommand, UserDto>
// but doesn't extend a marker interface
mediator.ExcludeBehavior<CreateUserValidation>() // Compilation error!
```

**After:** You can now exclude any typed behavior by its concrete type:
```csharp
// Exclude typed behavior by concrete type ✅
mediator.ExcludeTypedBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
    .SendAsync(new CreateUserCommand("Test", "test@test.com"));
```

---

## 🔧 Implementation

### **Files Modified:**

1. **`src/MediatorExtensions.cs`**
   - Added `ExcludeTypedBehavior<TBehavior, TRequest, TResponse>()` extension method
   - Improved documentation for `ExcludeBehavior` to clarify it requires marker interfaces

2. **`src/Pipeline/PipelineBuilder.cs`**
   - Added `ExcludeTypedBehavior<TBehavior, TRequest, TResponse>()` builder method
   - Delegates to `PipelineConfig.WithExcludedTyped()`

3. **`src/Pipeline/PipelineConfig.cs`**
   - Added `ExcludedTypedBehaviors` property (`IReadOnlySet<Type>`)
   - Added `WithExcludedTyped(Type behaviorType)` method
   - Updated all constructor calls to include the new parameter

4. **`src/Registration/BehaviorRegistry.cs`**
   - Updated `GetBehaviors()` to accept `excludedTypedBehaviors` parameter
   - Updated `FilterList()` to check `excludedTypedBehaviors.Contains(reg.BehaviorType)`
   - Updated `CollectAndSort()` to pass through the new parameter

5. **`src/Internals/RequestHandlerWrapper.cs`**
   - Updated call to `GetBehaviors()` to include `pipelineConfig.ExcludedTypedBehaviors`

---

## 📚 Usage Examples

### **Basic Usage - Exclude Single Typed Behavior**
```csharp
// Exclude validation for this request
var result = await mediator
    .ExcludeTypedBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
    .SendAsync(new CreateUserCommand("", "test@test.com")); // Empty name allowed!
```

### **Chaining with ExcludeBehavior**
```csharp
// Exclude both marker-based and typed behaviors
await mediator
    .ExcludeBehavior<ILoggingBehavior>()           // Exclude by marker interface
    .ExcludeTypedBehavior<CreateUserValidation, CreateUserCommand, UserDto>() // Exclude by concrete type
    .SendAsync(new CreateUserCommand("Test", "test@test.com"));
```

### **Combining with Other Pipeline Controls**
```csharp
// Exclude typed behavior but keep global behaviors
await mediator
    .WithPipelineGroup("admin")
    .ExcludeTypedBehavior<CreateUserValidation, CreateUserCommand, UserDto>()
    .SendAsync(new CreateUserCommand("Test", "test@test.com"));
```

### **Skip Caching for Specific Request**
```csharp
// Bypass cache for this query
var result = await mediator
    .ExcludeTypedBehavior<GetUserCaching, GetUserQuery, UserDto?>()
    .SendAsync(new GetUserQuery(userId)); // Forces fresh fetch
```

---

## 🧪 Test Coverage

Created **`ExcludeTypedBehaviorTests.cs`** with **13 comprehensive tests**:

### **Core Functionality:**
- ✅ `ExcludeTypedBehavior_ExcludesSpecificTypedBehavior` - Basic exclusion works
- ✅ `ExcludeTypedBehavior_DoesNotAffectOtherTypedBehaviors` - Only excludes specified behavior
- ✅ `ExcludeTypedBehavior_DoesNotAffectGlobalBehaviors` - Global behaviors still execute
- ✅ `ExcludeTypedBehavior_AllowsInvalidDataWhenValidationExcluded` - Validation can be bypassed

### **Chaining & Composition:**
- ✅ `ExcludeTypedBehavior_CanBeChainedWithExcludeBehavior` - Works with marker exclusion
- ✅ `ExcludeTypedBehavior_MultipleExclusions` - Multiple typed behaviors can be excluded
- ✅ `ExcludeTypedBehavior_CombinedWithWithoutPipeline` - WithoutPipeline takes precedence
- ✅ `ExcludeTypedBehavior_CombinedWithSkipGlobalBehaviors` - Compatible with SkipGlobalBehaviors
- ✅ `ExcludeTypedBehavior_WithPipelineGroup` - Works with pipeline groups

### **Edge Cases:**
- ✅ `ExcludeTypedBehavior_CachingBehavior_SkipsCaching` - Cache bypass works correctly
- ✅ `ExcludeTypedBehavior_WithoutExclusion_ValidationThrowsOnInvalidData` - Normal validation works
- ✅ `ExcludeTypedBehavior_DifferentRequestType_BehaviorNotAffected` - Type-safe exclusion
- ✅ `ExcludeTypedBehavior_BehaviorOrder_MaintainedAfterExclusion` - Order preserved

---

## 🔍 How It Works

### **Filtering Logic:**

When building the pipeline, `BehaviorRegistry.FilterList()` now checks two exclusion sets:

```csharp
// Check marker interface exclusion (existing)
if (hasExclusions && HasExcludedMarker(reg, excludedMarkers))
    continue;

// Check typed behavior exclusion (NEW)
if (hasTypedExclusions && excludedTypedBehaviors.Contains(reg.BehaviorType))
    continue;
```

### **Type Safety:**

The method signature ensures type safety:
```csharp
public static PipelineBuilder ExcludeTypedBehavior<TBehavior, TRequest, TResponse>(this IMediator mediator)
    where TBehavior : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
```

This guarantees:
- `TBehavior` must implement `IPipelineBehavior<TRequest, TResponse>`
- `TRequest` must implement `IRequest<TResponse>`
- Type mismatch caught at compile time

---

## 📊 Test Results

**Total Tests:** 87  
**Pass Rate:** 100% ✅  
**New Tests Added:** 13  
**Tests Before:** 74  

### **Test Breakdown:**
- Original Tests: 74 (all passing)
- ExcludeTypedBehavior Tests: 13 (all passing)

---

## 🎯 Use Cases

### **1. Bypass Validation for System Operations**
```csharp
// Admin operation - skip normal validation
await mediator
    .ExcludeTypedBehavior<UserInputValidation, CreateUserCommand, UserDto>()
    .SendAsync(new CreateUserCommand(systemGeneratedName, systemEmail));
```

### **2. Force Cache Refresh**
```csharp
// Force fresh data from database
var freshData = await mediator
    .ExcludeTypedBehavior<CachingBehavior, GetUserQuery, UserDto?>()
    .SendAsync(new GetUserQuery(userId));
```

### **3. Testing Without Side Effects**
```csharp
// Test handler logic without notification side effects
await mediator
    .ExcludeTypedBehavior<NotificationBehavior, CreateOrderCommand, Order>()
    .SendAsync(testCommand);
```

### **4. Performance Testing**
```csharp
// Measure handler performance without logging overhead
await mediator
    .ExcludeBehavior<ILoggingBehavior>()
    .ExcludeTypedBehavior<AuditBehavior, UpdateCommand, Result>()
    .SendAsync(command);
```

---

## 🔄 Comparison: ExcludeBehavior vs ExcludeTypedBehavior

| Feature | `ExcludeBehavior<T>` | `ExcludeTypedBehavior<T, TReq, TRes>` |
|---------|---------------------|---------------------------------------|
| **Works with** | Marker interfaces | Concrete typed behaviors |
| **Constraint** | `where T : IPipelineBehavior` | `where T : IPipelineBehavior<TReq, TRes>` |
| **Use case** | Exclude all behaviors with marker | Exclude specific typed behavior |
| **Example** | `ExcludeBehavior<ILoggingBehavior>()` | `ExcludeTypedBehavior<CreateUserValidation, CreateUserCommand, UserDto>()` |
| **Type parameters** | 1 (behavior marker) | 3 (behavior, request, response) |

---

## 💡 Benefits

1. **Type Safety** - Compile-time verification of behavior, request, and response types
2. **Granular Control** - Exclude specific typed behaviors without affecting others
3. **Composable** - Chain with other pipeline controls (`ExcludeBehavior`, `SkipGlobalBehaviors`, etc.)
4. **No Marker Required** - Works with behaviors that don't implement marker interfaces
5. **Intuitive API** - Follows the same fluent pattern as existing methods

---

## 🚀 Backward Compatibility

✅ **100% Backward Compatible**
- All existing tests pass
- No breaking changes to existing API
- New feature is opt-in
- Existing `ExcludeBehavior<T>()` continues to work as before

---

## 📝 Summary

This feature fills a gap in the pipeline control API by allowing exclusion of typed behaviors that don't extend marker interfaces. It provides the same level of control for typed behaviors as `ExcludeBehavior<T>()` provides for marker-based behaviors, making the API more complete and consistent.
