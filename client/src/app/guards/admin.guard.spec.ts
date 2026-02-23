import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { adminGuard } from './admin.guard';
import { UserService } from '../services/user.service';

describe('adminGuard', () => {
  let userService: jasmine.SpyObj<UserService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const userServiceSpy = jasmine.createSpyObj('UserService', ['isLoggedIn', 'isAdmin']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    userService = TestBed.inject(UserService) as jasmine.SpyObj<UserService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(adminGuard).toBeTruthy();
  });
});
