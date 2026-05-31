import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  template: `
    <section class="placeholder">
      <p class="eyebrow">Placeholder</p>
      <h1>Dashboard</h1>
      <p>Dashboard metrics will be implemented in PR-11.</p>
    </section>
  `,
  styles: `
    .placeholder {
      display: grid;
      gap: 0.65rem;
      border: 1px solid #dbe3eb;
      border-radius: 8px;
      background: #ffffff;
      padding: 1.5rem;
    }

    .eyebrow,
    h1,
    p {
      margin: 0;
    }

    .eyebrow {
      color: #0f766e;
      font-size: 0.8rem;
      font-weight: 750;
      text-transform: uppercase;
    }

    h1 {
      color: #172026;
      font-size: 1.7rem;
      line-height: 1.2;
    }

    p {
      color: #4a5661;
      line-height: 1.5;
    }
  `,
})
export class DashboardComponent {}
