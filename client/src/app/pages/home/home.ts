import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, ButtonModule, Tooltip],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {

}
