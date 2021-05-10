﻿using System;
using System.Threading.Tasks;
using Bit.Core.Models;
using Microsoft.Azure.NotificationHubs;
using Bit.Core.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Bit.Core.Models;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;

namespace Bit.Core.Services
{
    public class NotificationHubPushNotificationService : IPushNotificationService
    {
        private readonly IInstallationDeviceRepository _installationDeviceRepository;
        private readonly GlobalSettings _globalSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private NotificationHubClient _client = null;

        public NotificationHubPushNotificationService(
            IInstallationDeviceRepository installationDeviceRepository,
            GlobalSettings globalSettings,
            IHttpContextAccessor httpContextAccessor)
        {
            _installationDeviceRepository = installationDeviceRepository;
            _globalSettings = globalSettings;
            _httpContextAccessor = httpContextAccessor;
            _client = NotificationHubClient.CreateClientFromConnectionString(
                _globalSettings.NotificationHub.ConnectionString,
                _globalSettings.NotificationHub.HubName);
        }

        public async Task PushSyncCipherCreateAsync(UserCipher cipher)
        {
            await PushCipherAsync(cipher, PushType.SyncCipherCreate);
        }

        public async Task PushSyncCipherUpdateAsync(UserCipher cipher)
        {
            await PushCipherAsync(cipher, PushType.SyncCipherUpdate);
        }

        public async Task PushSyncCipherDeleteAsync(UserCipher cipher)
        {
            await PushCipherAsync(cipher, PushType.SyncLoginDelete);
        }

        private async Task PushCipherAsync(UserCipher cipher, PushType type)
        {
            var message = new SyncCipherPushNotification
            {
                Id = cipher.Id,
                UserId = cipher.UserId,
                RevisionDate = cipher.RevisionDate,
            };

            await SendPayloadToUserAsync(cipher.UserId, type, message, true);
        }

        public async Task PushSyncFolderCreateAsync(Folder folder)
        {
            await PushFolderAsync(folder, PushType.SyncFolderCreate);
        }

        public async Task PushSyncFolderUpdateAsync(Folder folder)
        {
            await PushFolderAsync(folder, PushType.SyncFolderUpdate);
        }

        public async Task PushSyncFolderDeleteAsync(Folder folder)
        {
            await PushFolderAsync(folder, PushType.SyncFolderDelete);
        }

        private async Task PushFolderAsync(Folder folder, PushType type)
        {
            var message = new SyncFolderPushNotification
            {
                Id = folder.Id,
                UserId = folder.UserId,
                RevisionDate = folder.RevisionDate
            };

            await SendPayloadToUserAsync(folder.UserId, type, message, true);
        }

        public async Task PushSyncCiphersAsync(Guid userId)
        {
            await PushUserAsync(userId, PushType.SyncCiphers);
        }

        public async Task PushSyncVaultAsync(Guid userId)
        {
            await PushUserAsync(userId, PushType.SyncVault);
        }

        public async Task PushSyncOrgKeysAsync(Guid userId)
        {
            await PushUserAsync(userId, PushType.SyncOrgKeys);
        }

        public async Task PushSyncSettingsAsync(Guid userId)
        {
            await PushUserAsync(userId, PushType.SyncSettings);
        }

        public async Task PushLogOutAsync(Guid userId)
        {
            await PushUserAsync(userId, PushType.LogOut);
        }

        private async Task PushUserAsync(Guid userId, PushType type)
        {
            var message = new UserPushNotification
            {
                UserId = userId,
                Date = DateTime.UtcNow
            };

            await SendPayloadToUserAsync(userId, type, message, false);
        }

        private async Task SendPayloadToUserAsync(Guid userId, PushType type, object payload, bool excludeCurrentContext)
        {
            await SendPayloadToUserAsync(userId.ToString(), type, payload, GetContextIdentifier(excludeCurrentContext));
        }

        private async Task SendPayloadToOrganizationAsync(Guid orgId, PushType type, object payload, bool excludeCurrentContext)
        {
            await SendPayloadToUserAsync(orgId.ToString(), type, payload, GetContextIdentifier(excludeCurrentContext));
        }

        public async Task SendPayloadToUserAsync(string userId, PushType type, object payload, string identifier,
            string deviceId = null)
        {
            var tag = BuildTag($"template:payload_userId:{userId}", identifier);
            await SendPayloadAsync(tag, type, payload);
            if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
            {
                await _installationDeviceRepository.UpsertAsync(new InstallationDeviceEntity(deviceId));
            }
        }

        public async Task SendPayloadToOrganizationAsync(string orgId, PushType type, object payload, string identifier,
            string deviceId = null)
        {
            var tag = BuildTag($"template:payload && organizationId:{orgId}", identifier);
            await SendPayloadAsync(tag, type, payload);
            if (InstallationDeviceEntity.IsInstallationDeviceId(deviceId))
            {
                await _installationDeviceRepository.UpsertAsync(new InstallationDeviceEntity(deviceId));
            }
        }

        private string GetContextIdentifier(bool excludeCurrentContext)
        {
            if (!excludeCurrentContext)
            {
                return null;
            }

            return _httpContextAccessor?.HttpContext?.DeviceIdentifier();
        }

        private string BuildTag(string tag, string identifier)
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                tag += $" && !deviceIdentifier:{identifier}";
            }

            return $"({tag})";
        }

        private async Task SendPayloadAsync(string tag, PushType type, object payload)
        {
            await _client.SendTemplateNotificationAsync(
                new Dictionary<string, string>
                {
                    { "type",  ((byte)type).ToString() },
                    { "payload", JsonConvert.SerializeObject(payload) }
                }, tag);
        }
    }
}
