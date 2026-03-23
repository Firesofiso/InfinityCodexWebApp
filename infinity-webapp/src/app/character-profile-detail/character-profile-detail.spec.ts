import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CharacterProfileDetail } from './character-profile-detail';

describe('CharacterProfileDetail', () => {
  let component: CharacterProfileDetail;
  let fixture: ComponentFixture<CharacterProfileDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CharacterProfileDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CharacterProfileDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
