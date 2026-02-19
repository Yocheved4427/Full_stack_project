import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductQuickView } from './product-quick-view';

describe('ProductQuickView', () => {
  let component: ProductQuickView;
  let fixture: ComponentFixture<ProductQuickView>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductQuickView]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductQuickView);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
