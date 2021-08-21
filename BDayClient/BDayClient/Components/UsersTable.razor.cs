using BDayClient.Features;
using BDayClient.Shared;
using Blazored.LocalStorage;
using Entities;
using Entities.DTO;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayClient.Components
{
    public partial class UsersTable
    {
		[Parameter]
        public List<UserLite> Users { get; set; }

        [Parameter]
        public string LoggedUser { get; set; }

        [Parameter]
        public EventCallback<UserLite> OnUpdate { get; set; }

        [Parameter]
        public EventCallback<UserLite> OnRemoveAdminRole { get; set; }

        [Parameter]
        public EventCallback<UserLite> OnDeleteUser { get; set; }

        [Parameter]
        public EventCallback<UserLite2StepsAuthDto> OnChange2StepsAuthorization { get; set; }

        private Confirmation _confirmation;
        private UserLite _userToDelete;

        private async Task OnChange(bool? value, UserLite user)
        {

            var userChange2StepsAuth = new UserLite2StepsAuthDto();
            userChange2StepsAuth.Email = user.Email;
            userChange2StepsAuth.TwoFactorEnabled = (bool)value;
            await OnChange2StepsAuthorization.InvokeAsync(userChange2StepsAuth);
        }

        private async Task UpdateUser(UserLite userToUpdate)
        {
            await OnUpdate.InvokeAsync(userToUpdate);
        }

        private async Task RemoveAdminRole(UserLite userToUpdate)
        {
            await OnRemoveAdminRole.InvokeAsync(userToUpdate);
        }

        private void CallConfirmationModal(UserLite userToDelete)
        {
            _userToDelete = userToDelete;
            _confirmation.Show();
        }

        private async Task DeleteUser()
        {
            _confirmation.Hide();
            await OnDeleteUser.InvokeAsync(_userToDelete);
        }
    }
}
