using BDayClient.Shared;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BDayClient.Components
{
    public partial class PersonTable
    {
        [Parameter]
        public List<Person> Persons { get; set; }

        [Parameter]
        public EventCallback<Guid> OnDelete { get; set; }

        public DateTime timeNow = DateTime.Today;
        public int age;
        public int ageNameDay;

        private Confirmation _confirmation;
        private Guid _personIdToDelete;

        private void CallConfirmationModal(Guid id)
        {
            _personIdToDelete = id;
            _confirmation.Show();
        }

        private async Task DeletePerson()
        {
            _confirmation.Hide();
            await OnDelete.InvokeAsync(_personIdToDelete);
        }
    }
}