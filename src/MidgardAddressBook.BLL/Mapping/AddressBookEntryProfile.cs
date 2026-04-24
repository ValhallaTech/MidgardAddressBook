using AutoMapper;
using MidgardAddressBook.Core.Dtos;
using MidgardAddressBook.Core.Models;

namespace MidgardAddressBook.BLL.Mapping;

/// <summary>
/// AutoMapper profile mapping between <see cref="AddressBookEntry"/> domain entities and
/// <see cref="AddressBookEntryDto"/> view models.
/// </summary>
public class AddressBookEntryProfile : Profile
{
    /// <summary>Initializes the mapping profile.</summary>
    public AddressBookEntryProfile()
    {
        CreateMap<AddressBookEntry, AddressBookEntryDto>();
        CreateMap<AddressBookEntryDto, AddressBookEntry>()
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            .ForMember(dest => dest.FileName, opt => opt.Ignore());
    }
}
