using Dapper;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Repositories.Interfaces;

namespace RegistrationFormProject.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _context;

        public UserRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<List<UserListVM>> GetAllUsersAsync()
        {
            string sql = @"
SELECT
    u.""UserId"",
    u.""FullName"",
    u.""UserName"",
    u.""EmailID"",
    u.""MobileNo"",
    u.""DOB"",
    u.""CreatedDate"",
    u.""IsActive"",
    u.""IsSuperAdmin"",
    r.""RoleName""
FROM ""UserMasters"" u
LEFT JOIN ""RoleMasters"" r
ON u.""RoleId"" = r.""RoleId""
ORDER BY u.""UserId""";

            using var connection =
                _context.CreateConnection();

            var users =
                await connection.QueryAsync<UserListVM>(sql);

            return users.ToList();
        }

        public async Task<UserMaster?>
            GetUserByIdAsync(int id)
        {
            var query =
                @"SELECT *
                  FROM ""UserMasters""
                  WHERE ""UserId"" = @Id";

            using var connection =
                _context.CreateConnection();

            return await connection
                .QueryFirstOrDefaultAsync<UserMaster>(
                    query,
                    new { Id = id });
        }

        public async Task<int>
            AddUserAsync(UserMaster user)
        {
            var query =
                @"INSERT INTO ""UserMasters""
                (
                    ""UserName"",
                    ""Password"",
                    ""RoleId""
                )
                VALUES
                (
                    @UserName,
                    @Password,
                    @RoleId
                )";

            using var connection =
                _context.CreateConnection();

            return await connection.ExecuteAsync(
                query,
                user);
        }

        public async Task<int>
            DeleteUserAsync(int id)
        {
            var query =
                @"DELETE FROM ""UserMasters""
                  WHERE ""UserId""=@Id";

            using var connection =
                _context.CreateConnection();

            return await connection.ExecuteAsync(
                query,
                new { Id = id });
        }
        public async Task<List<UserVerificationVM>> GetVerificationDashboardAsync()
        {
            string sql = @"
SELECT
    u.""UserId"",
    u.""FullName"",
    CAST(COUNT(d.""DocumentId"") AS INTEGER) AS ""TotalDocuments"",
    CAST(COUNT(CASE WHEN d.""IsVerified"" = TRUE THEN 1 END) AS INTEGER) AS ""VerifiedDocuments"",
    CAST(COUNT(CASE WHEN d.""NeedsReupload"" = TRUE THEN 1 END) AS INTEGER) AS ""ReuploadDocuments"",
    CASE
        WHEN u.""IsProfileVerified"" = TRUE THEN 'Verified'
        WHEN COUNT(CASE WHEN d.""NeedsReupload"" = TRUE THEN 1 END) > 0 THEN 'Re-upload Required'
        ELSE 'Pending'
    END AS ""VerificationStatus""
FROM ""UserMasters"" u
LEFT JOIN ""UserDocuments"" d
ON u.""UserId"" = d.""UserId""
WHERE u.""RoleId"" = 2
GROUP BY
    u.""UserId"",
    u.""FullName"",
    u.""IsProfileVerified""
ORDER BY u.""UserId"";
";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<UserVerificationVM>(sql);
            return result.ToList();
        }

        public async Task<UserMaster?> GetUserByUsernameAsync(
    string username)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
SELECT *
FROM ""UserMasters""
WHERE ""UserName"" = @Username";

            return await connection.QueryFirstOrDefaultAsync<UserMaster>(
                sql,
                new { Username = username });
        }

//        public async Task SaveResetTokenAsync(
//    int userId,
//    string token,
//    DateTime expiry)
//        {
//            using var connection =
//                _context.CreateConnection();

//            string sql = @"
//INSERT INTO ""PasswordResetTokens""
//(
//    ""UserId"",
//    ""Token"",
//    ""ExpiryTime"",
//    ""IsUsed""
//)
//VALUES
//(
//    @UserId,
//    @Token,
//    @ExpiryTime,
//    FALSE
//)";

//            await connection.ExecuteAsync(
//                sql,
//                new
//                {
//                    UserId = userId,
//                    Token = token,
//                    ExpiryTime = expiry
//                });
//        }

        public async Task<UserMaster?> GetUserByValidTokenAsync(
    string token)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
SELECT u.*
FROM ""PasswordResetTokens"" p
JOIN ""UserMasters"" u
ON p.""UserId"" = u.""UserId""
WHERE p.""Token"" = @Token
AND p.""IsUsed"" = FALSE
AND p.""ExpiryTime"" > NOW()";

            return await connection.QueryFirstOrDefaultAsync<UserMaster>(
                sql,
                new { Token = token });
        }

        public async Task MarkTokenAsUsedAsync(
    string token)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
UPDATE ""PasswordResetTokens""
SET ""IsUsed"" = TRUE
WHERE ""Token"" = @Token";

            await connection.ExecuteAsync(
                sql,
                new { Token = token });
        }

        public async Task UpdatePasswordAsync(
    int userId,
    string hashedPassword)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
UPDATE ""UserMasters""
SET ""Password"" = @Password
WHERE ""UserId"" = @UserId";

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    Password = hashedPassword
                });
        }

        public async Task SaveOtpAsync(
    int userId,
    string otp,
    DateTime expiry,
    string method)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
INSERT INTO ""PasswordResetOtps""
(
    ""UserId"",
    ""OTP"",
    ""ExpiryTime"",
    ""IsUsed"",
    ""Method""
)
VALUES
(
    @UserId,
    @OTP,
    @ExpiryTime,
    FALSE,
    @Method
)";

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    OTP = otp,
                    ExpiryTime = expiry,
                    Method = method
                });
        }

        public async Task<bool> ValidateOtpAsync(
    int userId,
    string otp)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
SELECT COUNT(*)
FROM ""PasswordResetOtps""
WHERE ""UserId"" = @UserId
AND ""OTP"" = @OTP
AND ""IsUsed"" = FALSE
AND ""ExpiryTime"" > NOW()";

            int count =
                await connection.ExecuteScalarAsync<int>(
                    sql,
                    new
                    {
                        UserId = userId,
                        OTP = otp
                    });

            return count > 0;
        }

        public async Task MarkOtpUsedAsync(
    int userId,
    string otp)
        {
            using var connection =
                _context.CreateConnection();

            string sql = @"
UPDATE ""PasswordResetOtps""
SET ""IsUsed"" = TRUE
WHERE ""UserId"" = @UserId
AND ""OTP"" = @OTP";

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    OTP = otp
                });
        }
    }
}