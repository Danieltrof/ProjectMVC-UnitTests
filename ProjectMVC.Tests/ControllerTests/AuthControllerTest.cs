using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectMVC.Controllers;
using ProjectMVC.DTO;
using ProjectMVC.Services.Interfaces;
using ProjectMVC.Models;
using System;
using System.Threading.Tasks;

namespace ProjectMVC.Tests
{
    [TestClass]
    public class AuthControllerTests
    {
        //Call to AuthController and Mocking dependencies
        private Mock<IAuthService> _authServiceMock;
        private Mock<ILogger<AuthController>> _loggerMock;
        private AuthController _authController;

        //Constructor
        [TestInitialize]
        public void Setup()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _authController = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        //Positive test for Get / return register view with empty user
        [TestMethod]
        public void Register_Get_ReturnsViewWithEmptyUserDto()
        {
            // Act
            var result = _authController.Register() as ViewResult;

            // Assert
            Assert.IsNotNull(result); // Ensure a result is returned
            Assert.IsInstanceOfType(result.Model, typeof(UserDto)); // Ensure the model is a UserDto
            var model = result.Model as UserDto;
            Assert.IsNotNull(model); // Ensure model is not null
            Assert.AreEqual(string.Empty, model.Id); // Ensure Id and rest of the inputs are empty
            Assert.AreEqual(string.Empty, model.UserName); 
            Assert.AreEqual(string.Empty, model.Email); 
            Assert.AreEqual(string.Empty, model.Name); 
            Assert.AreEqual(string.Empty, model.Password); 
            Assert.AreEqual(string.Empty, model.ConfirmPassword); 
        }

        //Register (post) positive tests, in other words: Test to simulate a successful registration
        [TestMethod]
        public async Task Register_Post_ValidData_RedirectsToPostIndex()
        {
            //Must be a valid userDto object according to "UserManager" from AuthRepository.cs. 
            //UserManager is part of ASP.NET Identity, and have requirements for the userDto object. E.g for password:
            //At least one uppercase letter, one lowercase letter, one number, one special character and a minimum length of 6 characters.
            var validDto = new UserDto
            {
                UserName = "validuser",
                Email = "valid@example.com",
                Password = "ValidPassword123!",
                ConfirmPassword = "ValidPassword123!"
            };

            // Mock the RegisterUserAsync method to simulate successful registration
            _authServiceMock.Setup(service => service.RegisterUserAsync(It.IsAny<UserDto>()))
                            .ReturnsAsync(new Result { Succeeded = true, SuccessMessage = "Registration successful" });

            // Act
            var result = await _authController.Register(validDto) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result); // Ensure a redirect was returned
            Assert.AreEqual("Index", result.ActionName); // Ensure it redirects the new user to the Post.Index view
            Assert.AreEqual("Post", result.ControllerName); // Ensure the correct controller is targeted
            Assert.IsTrue(_authController.TempData.ContainsKey("SuccessMessage")); // Ensure success message is in TempData
            Assert.AreEqual("Registration successful", _authController.TempData["SuccessMessage"]);
            Assert.AreEqual("login", _authController.TempData["MessageType"]); // Ensure MessageType is set to "login"
        }


        [TestMethod]
        public async Task Register_Post_InvalidData_ReturnsViewWithErrors()
        {
            //Making a invalid userDto object
            var invalidDto = new UserDto
            {
                UserName = "",  // Invalid because it's empty
                Email = "invalid",  // Invalid email format, doesnt contain @
                Password = "short",  // Invalid because it's too short, no uppercase letter etc.
                ConfirmPassword = "differentpassword"  // Passwords do not match
            };

            // Act
            var result = await _authController.Register(invalidDto) as ViewResult;

            // Assert
            Assert.IsNotNull(result); // Ensure the result is a ViewResult
            Assert.AreEqual(invalidDto, result.Model); // Ensure the model is returned to the view
            Assert.IsTrue(_authController.ModelState.ContainsKey(string.Empty)); // Ensure ModelState contains errors
            Assert.IsTrue(_authController.ModelState.ErrorCount > 0); // Ensure there are model errors
        }


        [TestMethod]
        public async Task Register_Post_ServiceFails_ReturnsViewWithError()
        {
            var validDto = new UserDto
            {
                UserName = "validuser",
                Email = "valid@example.com",
                Password = "ValidPassword123!",
                ConfirmPassword = "ValidPassword123!"
            };

            // Mock the RegisterUserAsync method to simulate a failure in the service
            _authServiceMock.Setup(service => service.RegisterUserAsync(It.IsAny<UserDto>()))
                            .ReturnsAsync(new Result
                            {
                                Succeeded = false,
                                Error = "Registration failed due to some error"
                            });

            var result = await _authController.Register(validDto) as ViewResult;

            Assert.IsNotNull(result); // Ensure the result is a ViewResult (since registration failed)
            Assert.AreEqual(validDto, result.Model); // Ensure the model is returned to the view
            Assert.IsTrue(_authController.ModelState.ContainsKey(string.Empty)); // Ensure ModelState contains errors
            Assert.IsTrue(_authController.ModelState.ErrorCount > 0); // Ensure there are model errors
            Assert.AreEqual("Registration failed due to some error", _authController.ModelState[string.Empty].Errors[0].ErrorMessage); // Ensure the error message matches the one returned by the service
        }


        [TestMethod]
        public async Task Register_Post_ExceptionThrown_RedirectsToError()
        {
            var validDto = new UserDto
            {
                UserName = "validuser",
                Email = "valid@example.com",
                Password = "ValidPassword123!",
                ConfirmPassword = "ValidPassword123!"
            };

            // Mock the RegisterUserAsync method to throw an exception
            _authServiceMock.Setup(service => service.RegisterUserAsync(It.IsAny<UserDto>()))
                            .ThrowsAsync(new Exception("An unexpected error occurred"));

            var result = await _authController.Register(validDto) as RedirectToActionResult;

            Assert.IsNotNull(result); // Ensure the result is a RedirectToActionResult (since exception is thrown)
            Assert.AreEqual("Error", result.ControllerName); // Ensure the redirection is to the Error controller
            Assert.AreEqual("Error", result.ActionName); // Ensure the redirection is to the Error action
        }

        //Verifying that the Login view is returned with an empty LoginDto (GET)
        [TestMethod]
        public void Login_Get_ReturnsViewWithEmptyLoginDto()
        {
            // Act
            var result = _authController.Login() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(LoginDto));
        }

        //Testing a successful login (Positive test)
        [TestMethod]
        public async Task Login_Post_ValidData_ReturnsRedirectToPostIndex()
        {
            // valid loginDto object
            var loginDto = new LoginDto { Email = "test@example.com", Password = "P@ssw0rd" };
            _authServiceMock
                .Setup(s => s.LoginUserAsync(loginDto))
                .ReturnsAsync(new Result { Succeeded = true, SuccessMessage = "Login successful" });

            // If valid loginDto object is sent, the user should be redirected to the Post.Index view
            var result = await _authController.Login(loginDto) as RedirectToActionResult;

            // Confirm that the user is redirected to the Post.Index view
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Post", result.ControllerName);
        }

        //Testing a failed login (Negative test)
        [TestMethod]
        public async Task Login_Post_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange: Create a LoginDto object with invalid data (e.g., empty email and password).
            var loginDto = new LoginDto
            {
                Email = "",  // Simulating empty email field
                Password = "" // Simulating empty password field
            };

            // Simulate adding an error to the model state for the invalid fields (empty email and password)
            _authController.ModelState.AddModelError("Email", "Email is required");
            _authController.ModelState.AddModelError("Password", "Password is required");

            // Act: Call the Login method on the controller with the invalid LoginDto
            var result = await _authController.Login(loginDto) as ViewResult;

            // Assert: Ensure the result is a ViewResult (i.e., the controller returns the view when model is invalid)
            Assert.IsNotNull(result);

            // Ensure that the invalid LoginDto is passed back to the view
            Assert.AreEqual(loginDto, result.Model);
        }

        //Tests failed login attempt
        [TestMethod]
        public async Task Login_Post_ServiceFails_ReturnsViewWithError()
        {
            // Mocking a failed login attempt
            var loginDto = new LoginDto { Email = "test@example.com", Password = "P@ssw0rd" };
            _authServiceMock
                .Setup(s => s.LoginUserAsync(loginDto))
                .ReturnsAsync(new Result { Succeeded = false, Error = "Login failed" });

            var result = await _authController.Login(loginDto) as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(loginDto, result.Model);
            Assert.IsTrue(_authController.ModelState.ContainsKey(string.Empty));
        }

        //Tests exception thrown during login
        [TestMethod]
        public async Task Login_Post_ExceptionThrown_RedirectsToError()
        {
            //Mocking an exception thrown during login
            var loginDto = new LoginDto { Email = "test@example.com", Password = "P@ssw0rd" };
            _authServiceMock.Setup(s => s.LoginUserAsync(loginDto)).Throws(new Exception("Unexpected error"));

            var result = await _authController.Login(loginDto) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.ActionName);
            Assert.AreEqual("Error", result.ControllerName);
        }

        //Tests a successful logout
        [TestMethod]
        public async Task Logout_Post_ReturnsRedirectToPostIndex()
        {
            // Mocking a successful logout
            _authServiceMock
                .Setup(s => s.LogoutUserAsync())
                .ReturnsAsync(new Result { Succeeded = true, SuccessMessage = "Logout successful" });

            var result = await _authController.Logout() as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Post", result.ControllerName);
        }

        //Tests exception thrown during logout
        [TestMethod]
        public async Task Logout_Post_ExceptionThrown_RedirectsToError()
        {
            // Mocking an exception thrown during logout
            _authServiceMock.Setup(s => s.LogoutUserAsync()).Throws(new Exception("Unexpected error"));

            var result = await _authController.Logout() as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.ActionName);
            Assert.AreEqual("Error", result.ControllerName);
        }
    }
}