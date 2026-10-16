using Xunit;

// Disable parallel test execution across classes since all integration tests share the same local test database (QuanLyBanHangCongNo_Test).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
