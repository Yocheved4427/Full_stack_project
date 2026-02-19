const welcome = document.querySelector(".welcome");
const currentUser = JSON.parse(sessionStorage.getItem('user'));
welcome.textContent = `welcome back ${currentUser.firstName}`;


async function updateUser() {
    try {
        const Email = document.querySelector("#Email").value;
        const FirstName = document.querySelector("#FirstName").value;
        const LastName = document.querySelector("#LastName").value;
        const password = document.querySelector("#password").value;

        console.log("Email:", Email);
        console.log("FirstName:", FirstName);
        console.log("LastName:", LastName);
        console.log("Password:", password);

        let currentUser = JSON.parse(sessionStorage.getItem('user'));
        if (!currentUser) {
            alert("No current user in sessionStorage");
            return;
        }

        const Id = currentUser.id;

        const data = {
            Id,
         
            Email: Email || currentUser.Email,
            FirstName: FirstName || currentUser.FirstName,
            LastName: LastName || currentUser.LastName,
            Password: password || currentUser.Password
        };

        console.log("PUT body:", JSON.stringify(data));

        const response = await fetch(`https://localhost:44386/api/User/${Id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (response.status == 400) {
            throw Error("Your password is too weak.")
        }
        if (!response.ok) {
            throw Error("Update failed: " + response.status);
        }
        if (response.ok) {
            
            currentUser.Email = data.Email;
            currentUser.FirstName = data.FirstName;
            currentUser.LastName = data.LastName;
            currentUser.Password = data.Password;

            sessionStorage.setItem('user', JSON.stringify(currentUser));
            alert("success");
        
        }
    }
    catch (e) {
        console.error(e);
        alert(e);
    }
}
